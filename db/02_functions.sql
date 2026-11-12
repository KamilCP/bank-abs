-- =============================================================================
-- Bank ABS — серверна частина: функції користувача, збережені процедури, подання
-- Скрипт 2 з 4.
-- Уся бізнес-логіка фінансових операцій виконується на сервері БД в одній транзакції:
-- клієнтська частина лише викликає функції fn_* і отримує повідомлення про помилки
-- (RAISE EXCEPTION, SQLSTATE P0001) у зрозумілому для користувача вигляді.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Контекст користувача і час
-- Програма на початку кожної транзакції встановлює set_config('bankabs.user_id', ...).
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_current_user_id() RETURNS integer
LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('bankabs.user_id', true), '')::integer
$$;

CREATE FUNCTION fn_current_user_login() RETURNS varchar
LANGUAGE sql STABLE AS $$
    SELECT coalesce(nullif(current_setting('bankabs.user_login', true), ''), current_user)
$$;

-- Поточний момент часу операції. Параметр bankabs.demo_time використовує тільки скрипт
-- початкових даних (04_seed.sql), щоб сформувати історію операцій за минулі дні.
CREATE FUNCTION fn_now() RETURNS timestamptz
LANGUAGE sql STABLE AS $$
    SELECT coalesce(nullif(current_setting('bankabs.demo_time', true), '')::timestamptz, now())
$$;

CREATE FUNCTION fn_today() RETURNS date
LANGUAGE sql STABLE AS $$
    SELECT (fn_now() AT TIME ZONE 'Europe/Kyiv')::date
$$;

-- Запис події в журнал системи від імені поточного користувача
CREATE FUNCTION fn_log_event(p_event_type text, p_table text, p_record text,
                             p_summary text, p_details jsonb DEFAULT NULL) RETURNS bigint
LANGUAGE sql AS $$
    INSERT INTO audit_log (event_time, user_id, user_login, event_type, table_name, record_id, summary, details, ip_address)
    VALUES (fn_now(), fn_current_user_id(), fn_current_user_login(), p_event_type, coalesce(p_table, ''),
            coalesce(p_record, ''), left(coalesce(p_summary, ''), 300), p_details,
            coalesce(current_setting('bankabs.ip', true), ''))
    RETURNING id
$$;

-- -----------------------------------------------------------------------------
-- Параметри системи
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_param_text(p_key varchar) RETURNS text
LANGUAGE plpgsql STABLE AS $$
DECLARE
    v_value text;
BEGIN
    SELECT value INTO v_value FROM system_parameter WHERE key = p_key;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Не знайдено системний параметр %', p_key;
    END IF;
    RETURN v_value;
END
$$;

CREATE FUNCTION fn_param_numeric(p_key varchar) RETURNS numeric
LANGUAGE sql STABLE AS $$
    SELECT fn_param_text(p_key)::numeric
$$;

-- Форматування суми для повідомлень користувачу: 1234567.5 → «1 234 567,50»
CREATE FUNCTION fn_fmt_money(p_amount numeric) RETURNS text
LANGUAGE sql IMMUTABLE AS $$
    SELECT replace(regexp_replace(to_char(round(p_amount, 2), 'FM9999999999990.00'), '(\d)(?=(\d{3})+\.)', '\1 ', 'g'), '.', ',')
$$;

-- -----------------------------------------------------------------------------
-- Номер рахунку IBAN: UA + 2 контрольні цифри + код банку (6) + номер рахунку (19).
-- Контрольні цифри за ISO 13616 (MOD 97-10).
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_iban_check_digits(p_bank_code varchar, p_account varchar) RETURNS char(2)
LANGUAGE sql IMMUTABLE AS $$
    -- 'UA' переноситься в кінець і замінюється цифрами: U = 30, A = 10
    SELECT lpad((98 - ((p_bank_code || p_account || '301000')::numeric % 97))::text, 2, '0')
$$;

CREATE FUNCTION fn_generate_account_number(p_client_type char, p_account_type varchar) RETURNS char(29)
LANGUAGE plpgsql AS $$
DECLARE
    v_bank_code   varchar := fn_param_text('bank.code');
    v_balance_acc varchar;
    v_account     varchar;
BEGIN
    -- Балансовий рахунок за Планом рахунків НБУ
    v_balance_acc := CASE p_account_type || p_client_type
                         WHEN 'currentP' THEN '2620' WHEN 'currentL' THEN '2600'
                         WHEN 'depositP' THEN '2630' WHEN 'depositL' THEN '2610'
                         WHEN 'creditP'  THEN '2203' WHEN 'creditL'  THEN '2062'
                     END;
    v_account := lpad(v_balance_acc || lpad(nextval('account_number_seq')::text, 10, '0'), 19, '0');
    RETURN 'UA' || fn_iban_check_digits(v_bank_code, v_account) || v_bank_code || v_account;
END
$$;

-- -----------------------------------------------------------------------------
-- Конвертація валют за офіційним курсом з урахуванням маржі банку
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_exchange_rate(p_from char(3), p_to char(3)) RETURNS numeric
LANGUAGE plpgsql STABLE AS $$
DECLARE
    v_from   numeric;
    v_to     numeric;
    v_margin numeric;
BEGIN
    IF p_from = p_to THEN
        RETURN 1;
    END IF;
    SELECT rate_to_base INTO v_from FROM currency WHERE code = p_from;
    SELECT rate_to_base INTO v_to FROM currency WHERE code = p_to;
    IF v_from IS NULL OR v_to IS NULL THEN
        RAISE EXCEPTION 'Невідома валюта: % або %', p_from, p_to;
    END IF;
    v_margin := fn_param_numeric('fx.margin_percent');
    RETURN round(v_from / v_to * (1 - v_margin / 100), 6);
END
$$;

CREATE FUNCTION fn_convert(p_amount numeric, p_from char(3), p_to char(3)) RETURNS numeric
LANGUAGE sql STABLE AS $$
    SELECT round(p_amount * fn_exchange_rate(p_from, p_to), 2)
$$;

-- -----------------------------------------------------------------------------
-- Службові перевірки
-- -----------------------------------------------------------------------------
-- Сума операції: більша за нуль і не більше двох знаків після коми (копійки).
-- Інакше залишок рахунку (numeric(18,2)) і сума операції округлювалися б по-різному.
CREATE FUNCTION fn_check_amount(p_amount numeric) RETURNS void
LANGUAGE plpgsql IMMUTABLE AS $$
BEGIN
    IF p_amount IS NULL OR p_amount <= 0 THEN
        RAISE EXCEPTION 'Сума операції має бути більшою за нуль';
    END IF;
    IF p_amount <> round(p_amount, 2) THEN
        RAISE EXCEPTION 'Сума операції може містити не більше двох знаків після коми';
    END IF;
END
$$;

-- Повертає рахунок, заблокований для зміни (SELECT ... FOR UPDATE), або видає помилку
CREATE FUNCTION fn_lock_account(p_account_id integer, p_expected_type varchar DEFAULT NULL) RETURNS account
LANGUAGE plpgsql AS $$
DECLARE
    v_acc    account;
    v_status varchar;
BEGIN
    SELECT * INTO v_acc FROM account WHERE id = p_account_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Рахунок з ідентифікатором % не знайдено', p_account_id;
    END IF;
    IF v_acc.status <> 'open' THEN
        RAISE EXCEPTION 'Рахунок % закритий', v_acc.number;
    END IF;
    IF p_expected_type IS NOT NULL AND v_acc.account_type <> p_expected_type THEN
        RAISE EXCEPTION 'Операція недоступна для рахунку % (тип рахунку: %)', v_acc.number, v_acc.account_type;
    END IF;
    SELECT status INTO v_status FROM client WHERE id = v_acc.client_id;
    IF v_status <> 'active' THEN
        RAISE EXCEPTION 'Клієнт рахунку % заблокований, операції заборонені', v_acc.number;
    END IF;
    RETURN v_acc;
END
$$;

CREATE FUNCTION fn_post_operation(p_type varchar, p_from integer, p_to integer, p_amount numeric,
                                  p_currency char(3), p_description varchar,
                                  p_amount_to numeric DEFAULT NULL, p_currency_to char(3) DEFAULT NULL,
                                  p_rate numeric DEFAULT NULL) RETURNS bigint
LANGUAGE sql AS $$
    INSERT INTO operation (op_time, op_type, from_account_id, to_account_id, amount, currency_code,
                           amount_to, currency_to, rate, description, user_id)
    VALUES (fn_now(), p_type, p_from, p_to, p_amount, p_currency, p_amount_to, p_currency_to, p_rate,
            left(coalesce(p_description, ''), 250), fn_current_user_id())
    RETURNING id
$$;

-- -----------------------------------------------------------------------------
-- Рахунки
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_open_account(p_client_id integer, p_currency char(3), p_account_type varchar DEFAULT 'current')
RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE
    v_client client;
    v_id     integer;
BEGIN
    SELECT * INTO v_client FROM client WHERE id = p_client_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Клієнта з ідентифікатором % не знайдено', p_client_id;
    END IF;
    IF v_client.status <> 'active' THEN
        RAISE EXCEPTION 'Клієнт % заблокований, відкриття рахунку неможливе', v_client.full_name;
    END IF;
    INSERT INTO account (number, client_id, currency_code, account_type, opened_at)
    VALUES (fn_generate_account_number(v_client.client_type, p_account_type), p_client_id, p_currency,
            p_account_type, fn_today())
    RETURNING id INTO v_id;
    RETURN v_id;
END
$$;

CREATE FUNCTION fn_close_account(p_account_id integer) RETURNS void
LANGUAGE plpgsql AS $$
DECLARE
    v_acc account;
BEGIN
    v_acc := fn_lock_account(p_account_id, 'current');
    IF v_acc.balance <> 0 THEN
        RAISE EXCEPTION 'Рахунок % має залишок % %, закриття неможливе', v_acc.number, fn_fmt_money(v_acc.balance), v_acc.currency_code;
    END IF;
    IF EXISTS (SELECT 1 FROM deposit WHERE payout_account_id = p_account_id AND status = 'active')
       OR EXISTS (SELECT 1 FROM credit WHERE current_account_id = p_account_id AND status <> 'repaid') THEN
        RAISE EXCEPTION 'Рахунок % пов''язаний з діючим депозитом або кредитом', v_acc.number;
    END IF;
    UPDATE account SET status = 'closed', closed_at = fn_today() WHERE id = p_account_id;
END
$$;

-- -----------------------------------------------------------------------------
-- Касові операції і перекази
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_cash_in(p_account_id integer, p_amount numeric, p_description varchar DEFAULT '')
RETURNS bigint
LANGUAGE plpgsql AS $$
DECLARE
    v_acc account;
BEGIN
    PERFORM fn_check_amount(p_amount);
    v_acc := fn_lock_account(p_account_id, 'current');
    UPDATE account SET balance = balance + p_amount WHERE id = p_account_id;
    RETURN fn_post_operation('CASH_IN', NULL, p_account_id, p_amount, v_acc.currency_code,
                             coalesce(nullif(p_description, ''), 'Внесення готівки'));
END
$$;

CREATE FUNCTION fn_cash_out(p_account_id integer, p_amount numeric, p_description varchar DEFAULT '')
RETURNS bigint
LANGUAGE plpgsql AS $$
DECLARE
    v_acc account;
BEGIN
    PERFORM fn_check_amount(p_amount);
    v_acc := fn_lock_account(p_account_id, 'current');
    IF v_acc.balance < p_amount THEN
        RAISE EXCEPTION 'Недостатньо коштів на рахунку % (залишок % %)', v_acc.number, fn_fmt_money(v_acc.balance), v_acc.currency_code;
    END IF;
    UPDATE account SET balance = balance - p_amount WHERE id = p_account_id;
    RETURN fn_post_operation('CASH_OUT', p_account_id, NULL, p_amount, v_acc.currency_code,
                             coalesce(nullif(p_description, ''), 'Видача готівки'));
END
$$;

-- Переказ між поточними рахунками; якщо валюти різні — конвертація за курсом банку
CREATE FUNCTION fn_transfer(p_from_id integer, p_to_id integer, p_amount numeric, p_description varchar DEFAULT '')
RETURNS bigint
LANGUAGE plpgsql AS $$
DECLARE
    v_from      account;
    v_to        account;
    v_rate      numeric;
    v_amount_to numeric;
BEGIN
    PERFORM fn_check_amount(p_amount);
    IF p_from_id = p_to_id THEN
        RAISE EXCEPTION 'Рахунки відправника і отримувача збігаються';
    END IF;
    -- Блокуємо рахунки в порядку зростання id, щоб уникнути взаємного блокування
    IF p_from_id < p_to_id THEN
        v_from := fn_lock_account(p_from_id, 'current');
        v_to := fn_lock_account(p_to_id, 'current');
    ELSE
        v_to := fn_lock_account(p_to_id, 'current');
        v_from := fn_lock_account(p_from_id, 'current');
    END IF;
    IF v_from.balance < p_amount THEN
        RAISE EXCEPTION 'Недостатньо коштів на рахунку % (залишок % %)', v_from.number, fn_fmt_money(v_from.balance), v_from.currency_code;
    END IF;
    v_rate := fn_exchange_rate(v_from.currency_code, v_to.currency_code);
    v_amount_to := round(p_amount * v_rate, 2);
    IF v_amount_to <= 0 THEN
        RAISE EXCEPTION 'Сума після конвертації дорівнює нулю';
    END IF;
    UPDATE account SET balance = balance - p_amount WHERE id = p_from_id;
    UPDATE account SET balance = balance + v_amount_to WHERE id = p_to_id;
    IF v_from.currency_code = v_to.currency_code THEN
        RETURN fn_post_operation('TRANSFER', p_from_id, p_to_id, p_amount, v_from.currency_code,
                                 coalesce(nullif(p_description, ''), 'Переказ коштів'));
    END IF;
    RETURN fn_post_operation('TRANSFER', p_from_id, p_to_id, p_amount, v_from.currency_code,
                             coalesce(nullif(p_description, ''), 'Переказ з конвертацією'),
                             v_amount_to, v_to.currency_code, v_rate);
END
$$;

-- -----------------------------------------------------------------------------
-- Депозити
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_open_deposit(p_client_id integer, p_product_id integer, p_amount numeric, p_source_account_id integer)
RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE
    v_product deposit_product;
    v_source  account;
    v_acc_id  integer;
    v_id      integer;
BEGIN
    PERFORM fn_check_amount(p_amount);
    SELECT * INTO v_product FROM deposit_product WHERE id = p_product_id;
    IF NOT FOUND OR NOT v_product.is_active THEN
        RAISE EXCEPTION 'Депозитний продукт недоступний';
    END IF;
    IF p_amount < v_product.min_amount THEN
        RAISE EXCEPTION 'Мінімальна сума вкладу «%» — % %', v_product.name, fn_fmt_money(v_product.min_amount), v_product.currency_code;
    END IF;
    v_source := fn_lock_account(p_source_account_id, 'current');
    IF v_source.client_id <> p_client_id THEN
        RAISE EXCEPTION 'Рахунок списання % не належить клієнту', v_source.number;
    END IF;
    IF v_source.currency_code <> v_product.currency_code THEN
        RAISE EXCEPTION 'Валюта рахунку списання (%) не відповідає валюті вкладу (%)',
            v_source.currency_code, v_product.currency_code;
    END IF;
    IF v_source.balance < p_amount THEN
        RAISE EXCEPTION 'Недостатньо коштів на рахунку % (залишок % %)', v_source.number, fn_fmt_money(v_source.balance), v_source.currency_code;
    END IF;

    v_acc_id := fn_open_account(p_client_id, v_product.currency_code, 'deposit');
    UPDATE account SET balance = balance - p_amount WHERE id = p_source_account_id;
    UPDATE account SET balance = balance + p_amount WHERE id = v_acc_id;
    INSERT INTO deposit (client_id, product_id, account_id, payout_account_id, principal, annual_rate,
                         capitalization, start_date, end_date, last_accrual_date)
    VALUES (p_client_id, p_product_id, v_acc_id, p_source_account_id, p_amount, v_product.annual_rate,
            v_product.capitalization, fn_today(), (fn_today() + make_interval(months => v_product.term_months))::date,
            fn_today())
    RETURNING id INTO v_id;
    PERFORM fn_post_operation('DEPOSIT_OPEN', p_source_account_id, v_acc_id, p_amount, v_product.currency_code,
                              format('Розміщення вкладу «%s», договір № %s', v_product.name, v_id));
    RETURN v_id;
END
$$;

-- Нарахування відсотків за всіма діючими вкладами на дату p_on (щомісячно) і закриття
-- вкладів, строк яких завершився. Повертає кількість проведених нарахувань.
CREATE FUNCTION fn_accrue_deposit_interest(p_on date DEFAULT NULL) RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE
    v_on       date := coalesce(p_on, fn_today());
    v_dep      deposit;
    v_acc      account;
    v_next     date;
    v_base     numeric;
    v_interest numeric;
    v_count    integer := 0;
BEGIN
    FOR v_dep IN SELECT * FROM deposit WHERE status = 'active' ORDER BY id FOR UPDATE LOOP
        LOOP
            v_next := (v_dep.last_accrual_date + interval '1 month')::date;
            EXIT WHEN v_next > v_on OR v_next > v_dep.end_date;
            SELECT * INTO v_acc FROM account WHERE id = v_dep.account_id FOR UPDATE;
            v_base := CASE WHEN v_dep.capitalization THEN v_acc.balance ELSE v_dep.principal END;
            v_interest := round(v_base * v_dep.annual_rate / 100 / 12, 2);
            IF v_interest > 0 THEN
                IF v_dep.capitalization THEN
                    UPDATE account SET balance = balance + v_interest WHERE id = v_dep.account_id;
                    PERFORM fn_post_operation('DEPOSIT_INTEREST', NULL, v_dep.account_id, v_interest, v_acc.currency_code,
                        format('Капіталізація відсотків за вкладом № %s за період %s – %s', v_dep.id,
                               to_char(v_dep.last_accrual_date, 'DD.MM.YYYY'), to_char(v_next, 'DD.MM.YYYY')));
                ELSE
                    UPDATE account SET balance = balance + v_interest WHERE id = v_dep.payout_account_id;
                    PERFORM fn_post_operation('DEPOSIT_INTEREST', NULL, v_dep.payout_account_id, v_interest, v_acc.currency_code,
                        format('Виплата відсотків за вкладом № %s за період %s – %s', v_dep.id,
                               to_char(v_dep.last_accrual_date, 'DD.MM.YYYY'), to_char(v_next, 'DD.MM.YYYY')));
                END IF;
                v_count := v_count + 1;
            END IF;
            v_dep.last_accrual_date := v_next;
            v_dep.interest_paid := v_dep.interest_paid + v_interest;
            UPDATE deposit SET last_accrual_date = v_next, interest_paid = v_dep.interest_paid WHERE id = v_dep.id;
        END LOOP;

        -- Завершення строку вкладу: повернення коштів на рахунок виплати
        IF v_dep.end_date <= v_on THEN
            SELECT * INTO v_acc FROM account WHERE id = v_dep.account_id FOR UPDATE;
            IF v_acc.balance > 0 THEN
                UPDATE account SET balance = balance + v_acc.balance WHERE id = v_dep.payout_account_id;
                UPDATE account SET balance = 0 WHERE id = v_dep.account_id;
                PERFORM fn_post_operation('DEPOSIT_CLOSE', v_dep.account_id, v_dep.payout_account_id, v_acc.balance,
                    v_acc.currency_code, format('Повернення вкладу № %s після завершення строку', v_dep.id));
            END IF;
            UPDATE account SET status = 'closed', closed_at = v_on WHERE id = v_dep.account_id;
            UPDATE deposit SET status = 'closed', closed_at = v_on WHERE id = v_dep.id;
        END IF;
    END LOOP;
    RETURN v_count;
END
$$;

-- Дострокове розірвання вкладу: відсотки перераховуються за ставкою «до запитання»
CREATE FUNCTION fn_close_deposit_early(p_deposit_id integer) RETURNS numeric
LANGUAGE plpgsql AS $$
DECLARE
    v_dep          deposit;
    v_acc          account;
    v_product      deposit_product;
    v_days         integer;
    v_due_interest numeric;
    v_payout       numeric;
BEGIN
    SELECT * INTO v_dep FROM deposit WHERE id = p_deposit_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Вклад № % не знайдено', p_deposit_id;
    END IF;
    IF v_dep.status <> 'active' THEN
        RAISE EXCEPTION 'Вклад № % уже закритий', p_deposit_id;
    END IF;
    SELECT * INTO v_product FROM deposit_product WHERE id = v_dep.product_id;
    IF NOT v_product.early_withdrawal THEN
        RAISE EXCEPTION 'Умови вкладу «%» не передбачають дострокового повернення', v_product.name;
    END IF;
    PERFORM fn_lock_account(v_dep.payout_account_id, 'current');
    SELECT * INTO v_acc FROM account WHERE id = v_dep.account_id FOR UPDATE;

    v_days := fn_today() - v_dep.start_date;
    v_due_interest := round(v_dep.principal * fn_param_numeric('deposit.early_rate') / 100 * v_days / 365, 2);
    -- Для вкладу без капіталізації частину відсотків клієнт уже отримав на рахунок виплати
    v_payout := v_dep.principal + v_due_interest
                - CASE WHEN v_dep.capitalization THEN 0 ELSE v_dep.interest_paid END;
    v_payout := least(greatest(v_payout, v_dep.principal), v_acc.balance);

    UPDATE account SET balance = balance + v_payout WHERE id = v_dep.payout_account_id;
    PERFORM fn_post_operation('DEPOSIT_CLOSE', v_dep.account_id, v_dep.payout_account_id, v_payout, v_acc.currency_code,
        format('Дострокове повернення вкладу № %s', v_dep.id));
    IF v_acc.balance > v_payout THEN
        PERFORM fn_post_operation('DEPOSIT_CLOSE', v_dep.account_id, NULL, v_acc.balance - v_payout, v_acc.currency_code,
            format('Утримання відсотків при достроковому розірванні вкладу № %s', v_dep.id));
    END IF;
    UPDATE account SET balance = 0, status = 'closed', closed_at = fn_today() WHERE id = v_dep.account_id;
    UPDATE deposit SET status = 'closed_early', closed_at = fn_today() WHERE id = v_dep.id;
    RETURN v_payout;
END
$$;

-- -----------------------------------------------------------------------------
-- Кредити (ануїтетна схема погашення)
-- -----------------------------------------------------------------------------
CREATE FUNCTION fn_annuity_payment(p_amount numeric, p_annual_rate numeric, p_months integer) RETURNS numeric
LANGUAGE plpgsql IMMUTABLE AS $$
DECLARE
    v_rate numeric := p_annual_rate / 100 / 12;
BEGIN
    IF p_months <= 0 THEN
        RAISE EXCEPTION 'Строк кредиту має бути більшим за нуль';
    END IF;
    IF v_rate = 0 THEN
        RETURN round(p_amount / p_months, 2);
    END IF;
    RETURN round(p_amount * v_rate / (1 - power(1 + v_rate, -p_months)), 2);
END
$$;

CREATE FUNCTION fn_issue_credit(p_client_id integer, p_product_id integer, p_amount numeric,
                                p_term_months integer, p_current_account_id integer) RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE
    v_product   credit_product;
    v_current   account;
    v_acc_id    integer;
    v_id        integer;
    v_payment   numeric;
    v_rate      numeric;
    v_balance   numeric;
    v_interest  numeric;
    v_principal numeric;
    v_issue     date := fn_today();
BEGIN
    PERFORM fn_check_amount(p_amount);
    SELECT * INTO v_product FROM credit_product WHERE id = p_product_id;
    IF NOT FOUND OR NOT v_product.is_active THEN
        RAISE EXCEPTION 'Кредитний продукт недоступний';
    END IF;
    IF p_amount < v_product.min_amount OR p_amount > v_product.max_amount THEN
        RAISE EXCEPTION 'Сума кредиту «%» має бути від % до % %', v_product.name,
            fn_fmt_money(v_product.min_amount), fn_fmt_money(v_product.max_amount), v_product.currency_code;
    END IF;
    IF p_term_months < v_product.min_term_months OR p_term_months > v_product.max_term_months THEN
        RAISE EXCEPTION 'Строк кредиту «%» має бути від % до % міс.', v_product.name,
            v_product.min_term_months, v_product.max_term_months;
    END IF;
    v_current := fn_lock_account(p_current_account_id, 'current');
    IF v_current.client_id <> p_client_id THEN
        RAISE EXCEPTION 'Рахунок зарахування % не належить клієнту', v_current.number;
    END IF;
    IF v_current.currency_code <> v_product.currency_code THEN
        RAISE EXCEPTION 'Валюта рахунку зарахування (%) не відповідає валюті кредиту (%)',
            v_current.currency_code, v_product.currency_code;
    END IF;
    IF EXISTS (SELECT 1 FROM credit WHERE client_id = p_client_id AND status = 'overdue') THEN
        RAISE EXCEPTION 'Клієнт має прострочену заборгованість, видача нового кредиту заборонена';
    END IF;

    v_payment := fn_annuity_payment(p_amount, v_product.annual_rate, p_term_months);
    v_acc_id := fn_open_account(p_client_id, v_product.currency_code, 'credit');
    UPDATE account SET balance = p_amount WHERE id = v_acc_id;
    UPDATE account SET balance = balance + p_amount WHERE id = p_current_account_id;

    INSERT INTO credit (client_id, product_id, account_id, current_account_id, amount, annual_rate,
                        term_months, monthly_payment, issue_date)
    VALUES (p_client_id, p_product_id, v_acc_id, p_current_account_id, p_amount, v_product.annual_rate,
            p_term_months, v_payment, v_issue)
    RETURNING id INTO v_id;

    -- Графік погашення: відсотки на залишок, решта платежу — тіло кредиту;
    -- останній платіж вирівнюється на залишок, що утворився через округлення
    v_rate := v_product.annual_rate / 100 / 12;
    v_balance := p_amount;
    FOR i IN 1 .. p_term_months LOOP
        v_interest := round(v_balance * v_rate, 2);
        v_principal := CASE WHEN i = p_term_months THEN v_balance ELSE v_payment - v_interest END;
        v_balance := v_balance - v_principal;
        INSERT INTO credit_schedule (credit_id, payment_no, due_date, payment, principal_part, interest_part, balance_after)
        VALUES (v_id, i, (v_issue + make_interval(months => i))::date, v_principal + v_interest,
                v_principal, v_interest, v_balance);
    END LOOP;

    PERFORM fn_post_operation('CREDIT_ISSUE', v_acc_id, p_current_account_id, p_amount, v_product.currency_code,
        format('Видача кредиту «%s», договір № %s', v_product.name, v_id));
    RETURN v_id;
END
$$;

-- Сплата чергового платежу за графіком з поточного рахунку клієнта
CREATE FUNCTION fn_credit_payment(p_credit_id integer) RETURNS bigint
LANGUAGE plpgsql AS $$
DECLARE
    v_credit  credit;
    v_item    credit_schedule;
    v_current account;
    v_op_id   bigint;
BEGIN
    SELECT * INTO v_credit FROM credit WHERE id = p_credit_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Кредит № % не знайдено', p_credit_id;
    END IF;
    IF v_credit.status = 'repaid' THEN
        RAISE EXCEPTION 'Кредит № % уже погашено', p_credit_id;
    END IF;
    SELECT * INTO v_item FROM credit_schedule
     WHERE credit_id = p_credit_id AND paid_at IS NULL
     ORDER BY payment_no LIMIT 1 FOR UPDATE;
    v_current := fn_lock_account(v_credit.current_account_id, 'current');
    IF v_current.balance < v_item.payment THEN
        RAISE EXCEPTION 'Недостатньо коштів для сплати платежу № % (потрібно % %, на рахунку % %)',
            v_item.payment_no, fn_fmt_money(v_item.payment), v_current.currency_code, fn_fmt_money(v_current.balance),
            v_current.currency_code;
    END IF;

    UPDATE account SET balance = balance - v_item.payment WHERE id = v_credit.current_account_id;
    UPDATE account SET balance = balance - v_item.principal_part WHERE id = v_credit.account_id;
    UPDATE credit_schedule SET paid_at = fn_now()
     WHERE credit_id = p_credit_id AND payment_no = v_item.payment_no;
    v_op_id := fn_post_operation('CREDIT_PAYMENT', v_credit.current_account_id, v_credit.account_id, v_item.payment,
        v_current.currency_code,
        format('Платіж № %s за кредитом № %s (тіло %s, відсотки %s)', v_item.payment_no, v_credit.id,
               v_item.principal_part, v_item.interest_part));

    IF NOT EXISTS (SELECT 1 FROM credit_schedule WHERE credit_id = p_credit_id AND paid_at IS NULL) THEN
        UPDATE account SET status = 'closed', closed_at = fn_today() WHERE id = v_credit.account_id;
        UPDATE credit SET status = 'repaid', closed_at = fn_today() WHERE id = p_credit_id;
    ELSIF NOT EXISTS (SELECT 1 FROM credit_schedule
                       WHERE credit_id = p_credit_id AND paid_at IS NULL AND due_date < fn_today()) THEN
        UPDATE credit SET status = 'active' WHERE id = p_credit_id AND status = 'overdue';
    END IF;
    RETURN v_op_id;
END
$$;

-- Позначення прострочених кредитів (неоплачений платіж з датою раніше p_on)
CREATE FUNCTION fn_refresh_overdue_credits(p_on date DEFAULT NULL) RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE
    v_on    date := coalesce(p_on, fn_today());
    v_count integer;
BEGIN
    UPDATE credit c SET status = 'overdue'
     WHERE c.status = 'active'
       AND EXISTS (SELECT 1 FROM credit_schedule s
                    WHERE s.credit_id = c.id AND s.paid_at IS NULL AND s.due_date < v_on);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END
$$;

-- Збережена процедура закриття операційного дня: нарахування відсотків за вкладами,
-- оновлення статусів кредитів, запис у журнал. Виклик: CALL sp_close_day(NULL);
CREATE PROCEDURE sp_close_day(p_on date)
LANGUAGE plpgsql AS $$
DECLARE
    v_on       date := coalesce(p_on, fn_today());
    v_accruals integer;
    v_overdue  integer;
BEGIN
    v_accruals := fn_accrue_deposit_interest(v_on);
    v_overdue := fn_refresh_overdue_credits(v_on);
    PERFORM fn_log_event('ACTION', 'operation', '', format('Закриття дня %s', to_char(v_on, 'DD.MM.YYYY')),
                         jsonb_build_object('date', v_on, 'deposit_accruals', v_accruals, 'new_overdue_credits', v_overdue));
END
$$;

-- -----------------------------------------------------------------------------
-- Звітні функції
-- -----------------------------------------------------------------------------
-- Виписка по рахунку за період з наростаючим залишком (поточні та депозитні рахунки)
CREATE FUNCTION fn_account_statement(p_account_id integer, p_from date, p_to date)
RETURNS TABLE (op_id bigint, op_time timestamptz, op_type varchar, description varchar,
               counterparty varchar, debit numeric, credit numeric, balance_after numeric)
LANGUAGE plpgsql STABLE AS $$
DECLARE
    v_acc     account;
    v_opening numeric;
BEGIN
    SELECT * INTO v_acc FROM account WHERE id = p_account_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Рахунок з ідентифікатором % не знайдено', p_account_id;
    END IF;
    IF v_acc.account_type = 'credit' THEN
        RAISE EXCEPTION 'Для кредитного рахунку сформуйте графік погашення кредиту';
    END IF;
    -- Вхідний залишок = поточний залишок мінус рух коштів після початку періоду
    SELECT v_acc.balance - coalesce(sum(CASE WHEN o.to_account_id = p_account_id THEN coalesce(o.amount_to, o.amount) ELSE 0 END
                                      - CASE WHEN o.from_account_id = p_account_id THEN o.amount ELSE 0 END), 0)
      INTO v_opening
      FROM operation o
     WHERE (o.from_account_id = p_account_id OR o.to_account_id = p_account_id)
       AND (o.op_time AT TIME ZONE 'Europe/Kyiv')::date >= p_from;

    RETURN QUERY
    SELECT m.id, m.op_time, m.op_type, m.description, m.counterparty, m.debit, m.credit,
           v_opening + sum(m.credit - m.debit) OVER (ORDER BY m.op_time, m.id)
      FROM (SELECT o.id, o.op_time, o.op_type, o.description,
                   coalesce(CASE WHEN o.to_account_id = p_account_id THEN fa.number ELSE ta.number END, '')::varchar AS counterparty,
                   CASE WHEN o.from_account_id = p_account_id THEN o.amount ELSE 0 END AS debit,
                   CASE WHEN o.to_account_id = p_account_id THEN coalesce(o.amount_to, o.amount) ELSE 0 END AS credit
              FROM operation o
              LEFT JOIN account fa ON fa.id = o.from_account_id
              LEFT JOIN account ta ON ta.id = o.to_account_id
             WHERE (o.from_account_id = p_account_id OR o.to_account_id = p_account_id)
               AND (o.op_time AT TIME ZONE 'Europe/Kyiv')::date >= p_from) m
     WHERE (m.op_time AT TIME ZONE 'Europe/Kyiv')::date <= p_to
     ORDER BY m.op_time, m.id;
END
$$;

-- Вхідний залишок рахунку на початок дати (для заголовка виписки)
CREATE FUNCTION fn_account_balance_on(p_account_id integer, p_date date) RETURNS numeric
LANGUAGE sql STABLE AS $$
    SELECT a.balance - coalesce((
        SELECT sum(CASE WHEN o.to_account_id = a.id THEN coalesce(o.amount_to, o.amount) ELSE 0 END
                 - CASE WHEN o.from_account_id = a.id THEN o.amount ELSE 0 END)
          FROM operation o
         WHERE (o.from_account_id = a.id OR o.to_account_id = a.id)
           AND (o.op_time AT TIME ZONE 'Europe/Kyiv')::date >= p_date), 0)
      FROM account a WHERE a.id = p_account_id
$$;

-- -----------------------------------------------------------------------------
-- Подання (views) для клієнтської частини
-- -----------------------------------------------------------------------------
CREATE VIEW v_account AS
SELECT a.id, a.number, a.client_id, c.full_name AS client_name, c.client_type, a.currency_code, a.account_type,
       a.balance, round(a.balance * cur.rate_to_base, 2) AS balance_base, a.status, a.opened_at, a.closed_at
  FROM account a
  JOIN client c ON c.id = a.client_id
  JOIN currency cur ON cur.code = a.currency_code;

CREATE VIEW v_operation AS
SELECT o.id, o.op_time, o.op_type, o.amount, o.currency_code, o.amount_to, o.currency_to, o.rate, o.description,
       o.from_account_id, fa.number AS from_number, fc.full_name AS from_client,
       o.to_account_id, ta.number AS to_number, tc.full_name AS to_client,
       o.user_id, coalesce(u.login, '') AS user_login,
       round(o.amount * cur.rate_to_base, 2) AS amount_base
  FROM operation o
  LEFT JOIN account fa ON fa.id = o.from_account_id
  LEFT JOIN client fc ON fc.id = fa.client_id
  LEFT JOIN account ta ON ta.id = o.to_account_id
  LEFT JOIN client tc ON tc.id = ta.client_id
  LEFT JOIN app_user u ON u.id = o.user_id
  JOIN currency cur ON cur.code = o.currency_code;

CREATE VIEW v_deposit AS
SELECT d.id, d.client_id, c.full_name AS client_name, d.product_id, p.name AS product_name, a.currency_code,
       d.account_id, a.number AS account_number, d.payout_account_id, pa.number AS payout_number,
       d.principal, a.balance AS current_amount, d.annual_rate, d.capitalization, d.start_date, d.end_date,
       d.last_accrual_date, d.interest_paid, d.status, d.closed_at, p.early_withdrawal
  FROM deposit d
  JOIN client c ON c.id = d.client_id
  JOIN deposit_product p ON p.id = d.product_id
  JOIN account a ON a.id = d.account_id
  JOIN account pa ON pa.id = d.payout_account_id;

CREATE VIEW v_credit AS
SELECT cr.id, cr.client_id, c.full_name AS client_name, cr.product_id, p.name AS product_name, a.currency_code,
       cr.account_id, a.number AS account_number, cr.current_account_id, ca.number AS current_number,
       cr.amount, a.balance AS debt, cr.annual_rate, cr.term_months, cr.monthly_payment, cr.issue_date,
       cr.status, cr.closed_at,
       (SELECT min(s.due_date) FROM credit_schedule s WHERE s.credit_id = cr.id AND s.paid_at IS NULL) AS next_due_date,
       (SELECT count(*) FROM credit_schedule s WHERE s.credit_id = cr.id AND s.paid_at IS NOT NULL)::integer AS paid_count
  FROM credit cr
  JOIN client c ON c.id = cr.client_id
  JOIN credit_product p ON p.id = cr.product_id
  JOIN account a ON a.id = cr.account_id
  JOIN account ca ON ca.id = cr.current_account_id;

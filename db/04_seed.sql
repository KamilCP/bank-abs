-- =============================================================================
-- Bank ABS — початкові дані
-- Скрипт 4 з 4: ролі і права, користувачі, параметри системи, довідники,
-- демонстраційні клієнти та історія операцій за останні 70 днів.
-- Демонстраційні клієнти вигадані; РНОКПП і коди ЄДРПОУ згенеровані з правильними
-- контрольними цифрами і не належать реальним особам.
-- =============================================================================

INSERT INTO schema_version (version, description) VALUES (1, 'Початкова схема Bank ABS 0.2.0');

-- -----------------------------------------------------------------------------
-- Ролі і права доступу до функцій
-- -----------------------------------------------------------------------------
INSERT INTO app_role (code, name, description, is_system) VALUES
    ('admin',    'Адміністратор',       'Усі функції системи, адміністрування користувачів, прав і параметрів', true),
    ('operator', 'Операціоніст',        'Обслуговування клієнтів: картки клієнтів, рахунки, касові операції, перекази', true),
    ('credit',   'Кредитний менеджер',  'Видача і супровід кредитів, депозити', true),
    ('manager',  'Керівник відділення', 'Перегляд усіх даних, звіти, статистика, довідники', true);

INSERT INTO app_permission (code, name, module, sort_order) VALUES
    ('clients.view',     'Перегляд клієнтів',                          'Клієнти',          10),
    ('clients.edit',     'Додавання і редагування клієнтів',           'Клієнти',          11),
    ('accounts.view',    'Перегляд рахунків',                          'Рахунки',          20),
    ('accounts.manage',  'Відкриття і закриття рахунків',              'Рахунки',          21),
    ('cash.operate',     'Касові операції (внесення, видача)',         'Операції',         30),
    ('transfers.make',   'Перекази між рахунками',                     'Операції',         31),
    ('operations.view',  'Перегляд журналу операцій',                  'Операції',         32),
    ('deposits.view',    'Перегляд депозитів',                         'Депозити',         40),
    ('deposits.manage',  'Відкриття, розірвання депозитів',            'Депозити',         41),
    ('credits.view',     'Перегляд кредитів',                          'Кредити',          50),
    ('credits.manage',   'Видача кредитів, прийом платежів',           'Кредити',          51),
    ('reports.view',     'Формування звітів',                          'Звіти і аналіз',   60),
    ('stats.view',       'Перегляд статистики',                        'Звіти і аналіз',   61),
    ('directories.edit', 'Редагування довідників (валюти, продукти)',  'Довідники',        70),
    ('admin.users',      'Ведення користувачів',                       'Адміністрування',  80),
    ('admin.roles',      'Налаштування ролей і прав доступу',          'Адміністрування',  81),
    ('admin.log',        'Перегляд журналу системи',                   'Адміністрування',  82),
    ('admin.settings',   'Системні параметри, закриття дня',           'Адміністрування',  83);

INSERT INTO role_permission (role_code, permission_code)
SELECT 'admin', code FROM app_permission;

INSERT INTO role_permission (role_code, permission_code) VALUES
    ('operator', 'clients.view'), ('operator', 'clients.edit'), ('operator', 'accounts.view'),
    ('operator', 'accounts.manage'), ('operator', 'cash.operate'), ('operator', 'transfers.make'),
    ('operator', 'operations.view'), ('operator', 'deposits.view'), ('operator', 'credits.view'),
    ('operator', 'reports.view'),
    ('credit', 'clients.view'), ('credit', 'accounts.view'), ('credit', 'operations.view'),
    ('credit', 'deposits.view'), ('credit', 'deposits.manage'), ('credit', 'credits.view'),
    ('credit', 'credits.manage'), ('credit', 'reports.view'),
    ('manager', 'clients.view'), ('manager', 'accounts.view'), ('manager', 'operations.view'),
    ('manager', 'deposits.view'), ('manager', 'credits.view'), ('manager', 'reports.view'),
    ('manager', 'stats.view'), ('manager', 'directories.edit');

-- Демонстраційні паролі (змініть після розгортання): admin / Admin123!, operator / Oper123!,
-- credit / Credit123!, manager / Manager123!, o.petrenko / Oper123!
INSERT INTO app_user (login, full_name, password_hash, role_code) VALUES
    ('admin',      'Сабіржанов Камільжан Рустамжанович', 'PBKDF2-SHA256$100000$ZPGRO6aKXCNw8vt5tHLqog==$PXHuonjkeKeDys/JAsmWPEhILtVuYwQ1utyAtwuJkTE=', 'admin'),
    ('operator',   'Іваненко Світлана Петрівна',         'PBKDF2-SHA256$100000$2QZsMC1v8UpiPsWM/yZinA==$bZEecwhY48RbaDu17itsclghfhwGuqAN/vLlqJNRu6w=', 'operator'),
    ('credit',     'Гнатюк Олег Васильович',             'PBKDF2-SHA256$100000$nTzvuDXSLDN8YBej7ZWtAA==$T9O9R/Y5a4Vi/lZNUAITdhHwIgMh1gnuWfecCrsVd5Y=', 'credit'),
    ('manager',    'Даниленко Ольга Миколаївна',         'PBKDF2-SHA256$100000$9tqc4EXAW9A7w+HaDyCxXA==$6U/NOUAE+FrtPPyYRGwQHV0ilLHaI3ongHdZ30UJQrE=', 'manager'),
    ('o.petrenko', 'Петренко Оксана Ігорівна',           'PBKDF2-SHA256$100000$iwiHtt+iF7cX6UP4CfbvWQ==$w9/oL2B1Ld9t/6Oar7gLy2/gQ/h6vit3xrJJr2CU/Ew=', 'operator');

-- -----------------------------------------------------------------------------
-- Системні параметри
-- -----------------------------------------------------------------------------
INSERT INTO system_parameter (key, value, value_type, name, category) VALUES
    ('bank.name',                  'АТ «Демо Банк»',                 'text',    'Назва банку (у звітах)',                         'Банк'),
    ('bank.code',                  '399001',                         'text',    'Код банку в IBAN (6 цифр, демонстраційний)',     'Банк'),
    ('bank.branch',                'Відділення № 1, м. Київ',        'text',    'Назва відділення',                               'Банк'),
    ('fx.margin_percent',          '1.5',                            'decimal', 'Маржа банку при конвертації валют, %',           'Операції'),
    ('deposit.early_rate',         '1.0',                            'decimal', 'Ставка при достроковому розірванні вкладу, %',   'Операції'),
    ('security.max_failed_logins', '5',                              'int',     'Кількість невдалих спроб входу до блокування',   'Безпека'),
    ('security.lockout_minutes',   '15',                             'int',     'Тривалість блокування після невдалих спроб, хв', 'Безпека'),
    ('security.session_minutes',   '30',                             'int',     'Час неактивності до завершення сеансу, хв',      'Безпека'),
    ('security.min_password_length','8',                             'int',     'Мінімальна довжина пароля',                      'Безпека'),
    ('ui.page_size',               '15',                             'int',     'Кількість рядків на сторінці таблиць',           'Інтерфейс'),
    ('ui.log_table_views',         'true',                           'bool',    'Записувати в журнал відкриття таблиць',          'Інтерфейс');

-- -----------------------------------------------------------------------------
-- Довідники
-- -----------------------------------------------------------------------------
INSERT INTO currency (code, name, symbol, is_base, rate_to_base) VALUES
    ('UAH', 'Українська гривня', '₴',  true,  1),
    ('USD', 'Долар США',         '$',  false, 41.70),
    ('EUR', 'Євро',              '€',  false, 48.40),
    ('PLN', 'Польський злотий',  'zł', false, 11.35);

INSERT INTO deposit_product (name, currency_code, annual_rate, term_months, capitalization, early_withdrawal, min_amount) VALUES
    ('Класичний 12',     'UAH', 13.50, 12, false, true,  1000),
    ('Капітал+',         'UAH', 14.50, 12, true,  false, 5000),
    ('Строковий 6',      'UAH', 12.00,  6, true,  true,  1000),
    ('Доларовий',        'USD',  2.50, 12, false, true,   100),
    ('Євро Ощадний',     'EUR',  1.50, 12, true,  true,   100);

INSERT INTO credit_product (name, currency_code, annual_rate, min_term_months, max_term_months, min_amount, max_amount) VALUES
    ('Споживчий',        'UAH', 29.90,  3, 36,   5000,   300000),
    ('Готівковий',       'UAH', 34.90,  1, 24,   1000,   100000),
    ('Бізнес-оборотний', 'UAH', 19.50,  6, 36,  50000,  3000000),
    ('Авто',             'UAH', 14.90, 12, 84, 100000,  1500000);

-- -----------------------------------------------------------------------------
-- Демонстраційні клієнти
-- -----------------------------------------------------------------------------
-- Клієнти зареєстровані в перший день історії (до відкриття рахунків), тому дата реєстрації
-- і запис аудиту передують операціям.
SELECT set_config('bankabs.demo_time', (((current_date - 70) + time '09:00') AT TIME ZONE 'Europe/Kyiv')::text, false),
       set_config('bankabs.user_id', (SELECT id FROM app_user WHERE login = 'operator')::text, false),
       set_config('bankabs.user_login', 'operator', false);
INSERT INTO client (client_type, full_name, tax_id, document, birth_date, phone, email, address, created_at) VALUES
    ('P', 'Коваленко Андрій Петрович', '3762629459', 'ID 519837288', '2003-01-06'::date, '+380671096465', 'andrii.kovalenko@example.com', 'м. Біла Церква, вул. Зелена, 37, кв. 118', ((current_date - 70) + time '09:00' + interval '0 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Шевчук Олена Василівна', '3421384640', 'ID 374417862', '1993-09-02'::date, '+380672024250', 'olena.shevchuk@example.com', 'м. Бровари, вул. Соборна, 36, кв. 114', ((current_date - 70) + time '09:00' + interval '95 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Бондаренко Ігор Миколайович', '2485204639', 'ID 726941481', '1968-01-16'::date, '+380674683153', 'ihor.bondarenko@example.com', 'м. Ірпінь, вул. Садова, 46, кв. 112', ((current_date - 70) + time '09:00' + interval '190 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Захарченко Марія Олегівна', '2528790800', 'ID 791854262', '1969-03-26'::date, '+380674579814', 'mariia.zakharchenko@example.com', 'м. Ірпінь, вул. Лісова, 30, кв. 58', ((current_date - 70) + time '09:00' + interval '285 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Кравченко Дмитро Сергійович', '2315651891', 'ID 790985870', '1963-05-26'::date, '+380675791247', 'dmytro.kravchenko@example.com', 'м. Біла Церква, просп. Перемоги, 88, кв. 70', ((current_date - 70) + time '09:00' + interval '380 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Олійник Наталія Іванівна', '2478920304', 'ID 276398349', '1967-11-14'::date, '+380677469953', 'nataliia.oliinyk@example.com', 'м. Бровари, вул. Миру, 86, кв. 141', ((current_date - 70) + time '09:00' + interval '475 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Мельник Олександр Вікторович', '2145800214', 'ID 801932459', '1958-10-01'::date, '+380678211420', 'oleksandr.melnyk@example.com', 'м. Біла Церква, вул. Садова, 103, кв. 16', ((current_date - 70) + time '09:00' + interval '570 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Савченко Ірина Андріївна', '2290726920', 'ID 575827813', '1962-09-19'::date, '+380674956332', 'iryna.savchenko@example.com', 'м. Ірпінь, вул. Соборна, 59, кв. 14', ((current_date - 70) + time '09:00' + interval '665 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Литвиненко Віктор Олександрович', '3047490594', 'ID 100373443', '1983-06-08'::date, '+380674277491', 'viktor.lytvynenko@example.com', 'м. Ірпінь, вул. Зелена, 90, кв. 82', ((current_date - 70) + time '09:00' + interval '760 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Мороз Тетяна Петрівна', '3591960445', 'ID 904536184', '1998-05-05'::date, '+380673591964', 'tetiana.moroz@example.com', 'м. Бориспіль, вул. Миру, 22, кв. 127', ((current_date - 70) + time '09:00' + interval '855 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Поліщук Роман Іванович', '2841899295', 'ID 348521083', '1977-10-21'::date, '+380678768379', 'roman.polishchuk@example.com', 'м. Біла Церква, вул. Центральна, 58, кв. 46', ((current_date - 70) + time '09:00' + interval '950 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Руденко Оксана Миколаївна', '3279722045', 'ID 541014034', '1989-10-17'::date, '+380676900620', 'oksana.rudenko@example.com', 'м. Ірпінь, вул. Шкільна, 18, кв. 54', ((current_date - 70) + time '09:00' + interval '1045 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Гончаренко Сергій Юрійович', '3364543250', 'ID 480819032', '1992-02-12'::date, '+380673099431', 'serhii.honcharenko@example.com', 'м. Київ, вул. Соборна, 5, кв. 179', ((current_date - 70) + time '09:00' + interval '1140 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('P', 'Левченко Юлія Дмитрівна', '3017115300', 'ID 488441967', '1982-08-09'::date, '+380675993465', 'yuliia.levchenko@example.com', 'м. Бровари, вул. Центральна, 110, кв. 37', ((current_date - 70) + time '09:00' + interval '1235 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('L', 'ТОВ «Вектор Плюс»', '32656385', 'Виписка з ЄДР', NULL, '+380447727525', 'info@vector-plus.example.com', 'м. Бориспіль, вул. Центральна, 48, оф. 20', ((current_date - 70) + time '09:00' + interval '1330 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('L', 'ТОВ «Агроресурс Південь»', '49500045', 'Виписка з ЄДР', NULL, '+380445355913', 'office@agroresurs.example.com', 'м. Біла Церква, вул. Центральна, 105, оф. 7', ((current_date - 70) + time '09:00' + interval '1425 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('L', 'ПП «Будмайстер-Сервіс»', '35721411', 'Виписка з ЄДР', NULL, '+380445925650', 'budmaister@example.com', 'м. Київ, вул. Центральна, 21, оф. 18', ((current_date - 70) + time '09:00' + interval '1520 seconds') AT TIME ZONE 'Europe/Kyiv'),
    ('L', 'ТОВ «ІТ Рішення Дніпро»', '32201213', 'Виписка з ЄДР', NULL, '+380446062366', 'hello@it-rishennia.example.com', 'м. Київ, вул. Садова, 100, оф. 13', ((current_date - 70) + time '09:00' + interval '1615 seconds') AT TIME ZONE 'Europe/Kyiv');
SELECT set_config('bankabs.demo_time', '', false),
       set_config('bankabs.user_id', '', false),
       set_config('bankabs.user_login', '', false);

-- -----------------------------------------------------------------------------
-- Історія операцій за останні 70 днів. Кожна операція виконується серверними
-- функціями (fn_cash_in, fn_transfer, fn_issue_credit, ...) з параметром
-- bankabs.demo_time, тому залишки, графіки і журнал узгоджені між собою.
-- -----------------------------------------------------------------------------
DO $seed$
DECLARE
    v_start    date := current_date - 70;
    v_day      date;
    v_client   record;
    v_acc      integer;
    v_acc2     integer;
    v_cr       record;
    v_n        integer;
    v_amount   numeric;
    v_operators integer[] := ARRAY(SELECT id FROM app_user WHERE role_code = 'operator' ORDER BY id);
    v_credit_user integer := (SELECT id FROM app_user WHERE login = 'credit');
    v_late_client integer;
    v_wd       integer := 0; -- номер робочого дня
BEGIN
    PERFORM setseed(0.581);

    -- День 0: відкриття поточних рахунків і початкові внески
    PERFORM set_config('bankabs.demo_time', ((v_start + time '09:30') AT TIME ZONE 'Europe/Kyiv')::text, true);
    PERFORM set_config('bankabs.user_id', v_operators[1]::text, true);
    PERFORM set_config('bankabs.user_login', 'operator', true);
    FOR v_client IN SELECT * FROM client ORDER BY id LOOP
        v_acc := fn_open_account(v_client.id, 'UAH', 'current');
        PERFORM fn_cash_in(v_acc, CASE WHEN v_client.client_type = 'L'
                                       THEN round((200000 + random() * 600000)::numeric, -2)
                                       ELSE round((8000 + random() * 70000)::numeric, -2) END, 'Початковий внесок');
        IF v_client.id % 3 = 0 THEN
            v_acc2 := fn_open_account(v_client.id, 'USD', 'current');
            PERFORM fn_cash_in(v_acc2, round((300 + random() * 4000)::numeric, 0), 'Внесення готівки');
        END IF;
        IF v_client.id % 5 = 0 THEN
            v_acc2 := fn_open_account(v_client.id, 'EUR', 'current');
            PERFORM fn_cash_in(v_acc2, round((200 + random() * 3000)::numeric, 0), 'Внесення готівки');
        END IF;
    END LOOP;
    v_late_client := (SELECT id FROM client WHERE client_type = 'P' ORDER BY id OFFSET 4 LIMIT 1);

    FOR v_n IN 1 .. 69 LOOP
        v_day := v_start + v_n;
        CONTINUE WHEN extract(isodow FROM v_day) = 7; -- неділя — вихідний
        v_wd := v_wd + 1;

        -- Вхід користувачів у систему
        PERFORM set_config('bankabs.demo_time', ((v_day + time '08:55') AT TIME ZONE 'Europe/Kyiv')::text, true);
        PERFORM set_config('bankabs.user_id', v_operators[1 + (v_n % array_length(v_operators, 1))]::text, true);
        PERFORM set_config('bankabs.user_login', (SELECT login FROM app_user WHERE id = v_operators[1 + (v_n % array_length(v_operators, 1))]), true);
        PERFORM fn_log_event('LOGIN', 'app_user', current_setting('bankabs.user_id'), 'Вхід у систему');

        -- Касові операції і перекази протягом дня
        FOR i IN 1 .. (3 + floor(random() * 6))::integer LOOP
            PERFORM set_config('bankabs.demo_time',
                ((v_day + time '09:00' + (random() * interval '8 hours')) AT TIME ZONE 'Europe/Kyiv')::text, true);
            SELECT id INTO v_acc FROM account WHERE account_type = 'current' AND status = 'open'
             ORDER BY random() LIMIT 1;
            v_amount := round((200 + random() * 9000)::numeric, -1);
            BEGIN
                CASE floor(random() * 4)::integer
                    WHEN 0 THEN PERFORM fn_cash_in(v_acc, v_amount, 'Внесення готівки');
                    WHEN 1 THEN PERFORM fn_cash_out(v_acc, round(least(v_amount, (SELECT balance FROM account WHERE id = v_acc) * 0.3), 2), 'Видача готівки');
                    ELSE
                        SELECT id INTO v_acc2 FROM account
                         WHERE account_type = 'current' AND status = 'open' AND id <> v_acc ORDER BY random() LIMIT 1;
                        PERFORM fn_transfer(v_acc, v_acc2, round(least(v_amount, (SELECT balance FROM account WHERE id = v_acc) * 0.25), 2),
                                            'Переказ коштів');
                END CASE;
            EXCEPTION WHEN OTHERS THEN
                NULL; -- недостатньо коштів тощо: операцію пропускаємо
            END;
        END LOOP;

        -- Депозити і кредити оформлює кредитний менеджер
        PERFORM set_config('bankabs.user_id', v_credit_user::text, true);
        PERFORM set_config('bankabs.user_login', 'credit', true);
        IF v_wd IN (3, 6, 10, 15, 21, 27, 34, 41) THEN
            PERFORM set_config('bankabs.demo_time', ((v_day + time '11:20') AT TIME ZONE 'Europe/Kyiv')::text, true);
            SELECT a.id, a.client_id, a.balance INTO v_cr FROM account a
             WHERE a.account_type = 'current' AND a.currency_code = 'UAH' AND a.status = 'open' AND a.balance > 12000
               AND NOT EXISTS (SELECT 1 FROM deposit d WHERE d.payout_account_id = a.id)
             ORDER BY a.balance DESC LIMIT 1 OFFSET (v_n % 3);
            IF FOUND THEN
                BEGIN
                    PERFORM fn_open_deposit(v_cr.client_id, CASE WHEN v_n % 2 = 0 THEN 1 ELSE 3 END,
                                            round(v_cr.balance * 0.5, -2), v_cr.id);
                EXCEPTION WHEN OTHERS THEN NULL;
                END;
            END IF;
        END IF;
        IF v_wd IN (2, 5, 9, 13, 18, 24, 31, 38, 45) THEN
            PERFORM set_config('bankabs.demo_time', ((v_day + time '14:10') AT TIME ZONE 'Europe/Kyiv')::text, true);
            SELECT a.id, a.client_id, c.client_type INTO v_cr FROM account a JOIN client c ON c.id = a.client_id
             WHERE a.account_type = 'current' AND a.currency_code = 'UAH' AND a.status = 'open'
               AND NOT EXISTS (SELECT 1 FROM credit k WHERE k.client_id = a.client_id)
             ORDER BY a.id LIMIT 1 OFFSET (v_n % 4);
            IF FOUND THEN
                BEGIN
                    IF v_cr.client_type = 'L' THEN
                        PERFORM fn_issue_credit(v_cr.client_id, 3, round((80000 + random() * 400000)::numeric, -3), 12, v_cr.id);
                    ELSE
                        PERFORM fn_issue_credit(v_cr.client_id, CASE WHEN v_n % 2 = 0 THEN 1 ELSE 2 END,
                                                round((10000 + random() * 60000)::numeric, -3), 6 + (v_n % 3) * 6, v_cr.id);
                    END IF;
                EXCEPTION WHEN OTHERS THEN NULL;
                END;
            END IF;
        END IF;

        -- Платежі за кредитами в день платежу (один клієнт допускає прострочку)
        PERFORM set_config('bankabs.user_id', v_operators[1]::text, true);
        PERFORM set_config('bankabs.user_login', 'operator', true);
        PERFORM set_config('bankabs.demo_time', ((v_day + time '16:30') AT TIME ZONE 'Europe/Kyiv')::text, true);
        FOR v_cr IN SELECT k.id, k.client_id, k.current_account_id, s.payment
                      FROM credit k JOIN credit_schedule s ON s.credit_id = k.id
                     WHERE k.status <> 'repaid' AND s.paid_at IS NULL AND s.due_date <= v_day
                       AND s.payment_no = (SELECT min(payment_no) FROM credit_schedule x
                                            WHERE x.credit_id = k.id AND x.paid_at IS NULL)
                     ORDER BY k.id LOOP
            CONTINUE WHEN v_cr.client_id = v_late_client;
            BEGIN
                IF (SELECT balance FROM account WHERE id = v_cr.current_account_id) < v_cr.payment THEN
                    PERFORM fn_cash_in(v_cr.current_account_id, round(v_cr.payment + 500, -2), 'Внесення для погашення кредиту');
                END IF;
                PERFORM fn_credit_payment(v_cr.id);
            EXCEPTION WHEN OTHERS THEN NULL;
            END;
        END LOOP;

        -- Закриття дня: нарахування відсотків за вкладами
        PERFORM set_config('bankabs.demo_time', ((v_day + time '18:05') AT TIME ZONE 'Europe/Kyiv')::text, true);
        PERFORM fn_accrue_deposit_interest(v_day);
        PERFORM fn_log_event('LOGOUT', 'app_user', current_setting('bankabs.user_id'), 'Вихід із системи');
    END LOOP;

    PERFORM fn_refresh_overdue_credits(current_date);
    PERFORM set_config('bankabs.demo_time', '', true);
    PERFORM set_config('bankabs.user_id', '', true);
    PERFORM set_config('bankabs.user_login', '', true);
END
$seed$;

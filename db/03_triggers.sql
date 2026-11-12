-- =============================================================================
-- Bank ABS — тригери
-- Скрипт 3 з 4: аудит змін даних, захист журналів від змін, службові поля.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Аудит: кожна зміна даних у таблицях системи записується в audit_log
-- з ідентифікатором користувача програми (параметр сеансу bankabs.user_id).
-- Для UPDATE зберігаються лише змінені поля у форматі {"поле": {"old": ..., "new": ...}}.
-- Значення поля password_hash у журнал не потрапляє.
-- -----------------------------------------------------------------------------
CREATE FUNCTION trg_audit_row() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_old     jsonb;
    v_new     jsonb;
    v_details jsonb := '{}'::jsonb;
    v_key     text;
    v_record  text;
    v_summary text;
BEGIN
    -- Службові поля не журналюються: updated_at, а також last_login_at (вхід фіксується подією LOGIN)
    IF TG_OP IN ('UPDATE', 'DELETE') THEN
        v_old := to_jsonb(OLD) - 'updated_at' - 'last_login_at';
    END IF;
    IF TG_OP IN ('INSERT', 'UPDATE') THEN
        v_new := to_jsonb(NEW) - 'updated_at' - 'last_login_at';
    END IF;
    IF v_old ? 'password_hash' THEN
        v_old := jsonb_set(v_old, '{password_hash}', '"***"');
    END IF;
    IF v_new ? 'password_hash' THEN
        v_new := jsonb_set(v_new, '{password_hash}', CASE WHEN TG_OP = 'UPDATE'
                     AND (to_jsonb(OLD) ->> 'password_hash') IS DISTINCT FROM (to_jsonb(NEW) ->> 'password_hash')
                     THEN '"*** (змінено)"' ELSE '"***"' END::jsonb);
    END IF;

    IF TG_OP = 'UPDATE' THEN
        FOR v_key IN SELECT jsonb_object_keys(v_new) LOOP
            IF v_new -> v_key IS DISTINCT FROM v_old -> v_key THEN
                v_details := v_details || jsonb_build_object(v_key,
                                 jsonb_build_object('old', v_old -> v_key, 'new', v_new -> v_key));
            END IF;
        END LOOP;
        IF v_details = '{}'::jsonb THEN
            RETURN NULL; -- нічого не змінилося
        END IF;
    ELSIF TG_OP = 'INSERT' THEN
        v_details := v_new;
    ELSE
        v_details := v_old;
    END IF;

    v_record := coalesce(v_new ->> 'id', v_old ->> 'id', v_new ->> 'code', v_old ->> 'code',
                         v_new ->> 'key', v_old ->> 'key',
                         concat_ws('/', coalesce(v_new ->> 'role_code', v_old ->> 'role_code'),
                                        coalesce(v_new ->> 'permission_code', v_old ->> 'permission_code')));
    v_summary := CASE TG_OP WHEN 'INSERT' THEN 'Додано запис' WHEN 'UPDATE' THEN 'Змінено поля: '
                     || (SELECT string_agg(k, ', ') FROM jsonb_object_keys(v_details) k) ELSE 'Видалено запис' END
                 || ' (' || TG_TABLE_NAME || ' ' || coalesce(v_record, '') || ')';

    PERFORM fn_log_event(TG_OP, TG_TABLE_NAME, v_record, v_summary, v_details);
    RETURN NULL;
END
$$;

CREATE TRIGGER trg_client_audit AFTER INSERT OR UPDATE OR DELETE ON client
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_operation_audit AFTER INSERT ON operation
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_deposit_audit AFTER INSERT OR UPDATE OR DELETE ON deposit
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_credit_audit AFTER INSERT OR UPDATE OR DELETE ON credit
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_app_user_audit AFTER INSERT OR UPDATE OR DELETE ON app_user
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_role_permission_audit AFTER INSERT OR DELETE ON role_permission
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_system_parameter_audit AFTER INSERT OR UPDATE OR DELETE ON system_parameter
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_currency_audit AFTER INSERT OR UPDATE OR DELETE ON currency
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_deposit_product_audit AFTER INSERT OR UPDATE OR DELETE ON deposit_product
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_credit_product_audit AFTER INSERT OR UPDATE OR DELETE ON credit_product
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();

-- Зміна балансу рахунку вже відображена в операції, тому окремий запис аудиту для неї
-- не створюється: оновлення, що змінюють лише balance, тригер аудиту рахунку пропускає.
CREATE TRIGGER trg_account_audit_ins_del AFTER INSERT OR DELETE ON account
    FOR EACH ROW EXECUTE FUNCTION trg_audit_row();
CREATE TRIGGER trg_account_audit_upd AFTER UPDATE ON account
    FOR EACH ROW
    WHEN ((to_jsonb(NEW) - 'balance' - 'updated_at') IS DISTINCT FROM (to_jsonb(OLD) - 'balance' - 'updated_at'))
    EXECUTE FUNCTION trg_audit_row();

-- -----------------------------------------------------------------------------
-- Незмінність фінансових операцій і журналу системи
-- -----------------------------------------------------------------------------
CREATE FUNCTION trg_forbid_change() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Записи таблиці % не можна змінювати або видаляти', TG_TABLE_NAME;
END
$$;

CREATE TRIGGER trg_operation_immutable BEFORE UPDATE OR DELETE ON operation
    FOR EACH ROW EXECUTE FUNCTION trg_forbid_change();
CREATE TRIGGER trg_audit_log_immutable BEFORE UPDATE OR DELETE ON audit_log
    FOR EACH ROW EXECUTE FUNCTION trg_forbid_change();

-- -----------------------------------------------------------------------------
-- Службові поля і правила цілісності
-- -----------------------------------------------------------------------------
CREATE FUNCTION trg_set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END
$$;

CREATE TRIGGER trg_client_updated_at BEFORE UPDATE ON client
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();
CREATE TRIGGER trg_account_updated_at BEFORE UPDATE ON account
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();
CREATE TRIGGER trg_system_parameter_updated_at BEFORE UPDATE ON system_parameter
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();
CREATE TRIGGER trg_currency_updated_at BEFORE UPDATE ON currency
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

-- Закрити можна лише рахунок з нульовим залишком
CREATE FUNCTION trg_account_close_check() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.status = 'closed' AND OLD.status = 'open' AND NEW.balance <> 0 THEN
        RAISE EXCEPTION 'Рахунок % має ненульовий залишок і не може бути закритий', NEW.number;
    END IF;
    IF NEW.status = 'open' AND OLD.status = 'closed' THEN
        RAISE EXCEPTION 'Закритий рахунок % не можна відкрити повторно', NEW.number;
    END IF;
    RETURN NEW;
END
$$;
CREATE TRIGGER trg_account_close_check BEFORE UPDATE OF status ON account
    FOR EACH ROW EXECUTE FUNCTION trg_account_close_check();

-- Перевірка формату значення системного параметра відповідно до його типу
CREATE FUNCTION trg_system_parameter_check() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.value_type = 'int' AND NEW.value !~ '^-?[0-9]+$' THEN
        RAISE EXCEPTION 'Параметр «%» має бути цілим числом', NEW.name;
    ELSIF NEW.value_type = 'decimal' AND NEW.value !~ '^-?[0-9]+(\.[0-9]+)?$' THEN
        RAISE EXCEPTION 'Параметр «%» має бути числом (роздільник — крапка)', NEW.name;
    ELSIF NEW.value_type = 'bool' AND NEW.value NOT IN ('true', 'false') THEN
        RAISE EXCEPTION 'Параметр «%» має бути true або false', NEW.name;
    END IF;
    RETURN NEW;
END
$$;
CREATE TRIGGER trg_system_parameter_check BEFORE INSERT OR UPDATE ON system_parameter
    FOR EACH ROW EXECUTE FUNCTION trg_system_parameter_check();

-- Користувач, що є останнім активним адміністратором, не може бути заблокований чи
-- переведений на іншу роль (захист від втрати доступу до адміністрування)
CREATE FUNCTION trg_app_user_last_admin() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.role_code = 'admin' AND OLD.is_active
       AND (NEW.role_code <> 'admin' OR NOT NEW.is_active)
       AND NOT EXISTS (SELECT 1 FROM app_user WHERE role_code = 'admin' AND is_active AND id <> OLD.id) THEN
        RAISE EXCEPTION 'Неможливо змінити роль або заблокувати останнього активного адміністратора';
    END IF;
    RETURN NEW;
END
$$;
CREATE TRIGGER trg_app_user_last_admin BEFORE UPDATE ON app_user
    FOR EACH ROW EXECUTE FUNCTION trg_app_user_last_admin();

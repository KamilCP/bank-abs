-- =============================================================================
-- Bank ABS — схема бази даних (PostgreSQL 16+)
-- Скрипт 1 з 4: таблиці, обмеження, індекси.
-- Порядок виконання: 01_schema.sql → 02_functions.sql → 03_triggers.sql → 04_seed.sql
-- Під час першого запуску програма виконує скрипти автоматично (DatabaseInitializer).
-- =============================================================================

CREATE TABLE schema_version (
    version      integer      PRIMARY KEY,
    applied_at   timestamptz  NOT NULL DEFAULT now(),
    description  text         NOT NULL
);

-- -----------------------------------------------------------------------------
-- Безпека: ролі, права доступу до функцій, користувачі
-- -----------------------------------------------------------------------------
CREATE TABLE app_role (
    code         varchar(20)  PRIMARY KEY,
    name         varchar(100) NOT NULL,
    description  text         NOT NULL DEFAULT '',
    is_system    boolean      NOT NULL DEFAULT false
);
COMMENT ON TABLE app_role IS 'Ролі користувачів (адміністратор, операціоніст, ...)';

CREATE TABLE app_permission (
    code         varchar(40)  PRIMARY KEY,
    name         varchar(150) NOT NULL,
    module       varchar(50)  NOT NULL,
    sort_order   integer      NOT NULL DEFAULT 0
);
COMMENT ON TABLE app_permission IS 'Функції системи, доступ до яких надається ролям';

CREATE TABLE role_permission (
    role_code        varchar(20) NOT NULL REFERENCES app_role (code) ON DELETE CASCADE,
    permission_code  varchar(40) NOT NULL REFERENCES app_permission (code) ON DELETE CASCADE,
    PRIMARY KEY (role_code, permission_code)
);

CREATE TABLE app_user (
    id               serial       PRIMARY KEY,
    login            varchar(50)  NOT NULL,
    full_name        varchar(150) NOT NULL,
    password_hash    varchar(200) NOT NULL,
    role_code        varchar(20)  NOT NULL REFERENCES app_role (code),
    is_active        boolean      NOT NULL DEFAULT true,
    failed_attempts  integer      NOT NULL DEFAULT 0,
    locked_until     timestamptz,
    last_login_at    timestamptz,
    created_at       timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_app_user_login CHECK (login ~ '^[a-z0-9_.]{3,50}$')
);
CREATE UNIQUE INDEX ux_app_user_login ON app_user (lower(login));
COMMENT ON TABLE app_user IS 'Користувачі системи (працівники банку)';

-- -----------------------------------------------------------------------------
-- Параметри системи і довідники
-- -----------------------------------------------------------------------------
CREATE TABLE system_parameter (
    key          varchar(60)  PRIMARY KEY,
    value        text         NOT NULL,
    value_type   varchar(10)  NOT NULL CHECK (value_type IN ('text', 'int', 'decimal', 'bool')),
    name         varchar(150) NOT NULL,
    category     varchar(50)  NOT NULL,
    updated_at   timestamptz  NOT NULL DEFAULT now()
);
COMMENT ON TABLE system_parameter IS 'Системні параметри роботи (редагує адміністратор)';

CREATE TABLE currency (
    code          char(3)       PRIMARY KEY,
    name          varchar(60)   NOT NULL,
    symbol        varchar(5)    NOT NULL,
    is_base       boolean       NOT NULL DEFAULT false,
    rate_to_base  numeric(14,6) NOT NULL CHECK (rate_to_base > 0),
    updated_at    timestamptz   NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_currency_base ON currency (is_base) WHERE is_base;
COMMENT ON COLUMN currency.rate_to_base IS 'Офіційний курс: скільки базової валюти (UAH) коштує 1 одиниця валюти';

CREATE TABLE deposit_product (
    id                 serial        PRIMARY KEY,
    name               varchar(100)  NOT NULL UNIQUE,
    currency_code      char(3)       NOT NULL REFERENCES currency (code),
    annual_rate        numeric(5,2)  NOT NULL CHECK (annual_rate >= 0 AND annual_rate <= 100),
    term_months        integer       NOT NULL CHECK (term_months BETWEEN 1 AND 120),
    capitalization     boolean       NOT NULL DEFAULT false,
    early_withdrawal   boolean       NOT NULL DEFAULT true,
    min_amount         numeric(18,2) NOT NULL DEFAULT 0 CHECK (min_amount >= 0),
    is_active          boolean       NOT NULL DEFAULT true
);

CREATE TABLE credit_product (
    id                 serial        PRIMARY KEY,
    name               varchar(100)  NOT NULL UNIQUE,
    currency_code      char(3)       NOT NULL REFERENCES currency (code),
    annual_rate        numeric(5,2)  NOT NULL CHECK (annual_rate >= 0 AND annual_rate <= 200),
    min_term_months    integer       NOT NULL DEFAULT 1 CHECK (min_term_months >= 1),
    max_term_months    integer       NOT NULL CHECK (max_term_months <= 360),
    min_amount         numeric(18,2) NOT NULL DEFAULT 0,
    max_amount         numeric(18,2) NOT NULL CHECK (max_amount > 0),
    is_active          boolean       NOT NULL DEFAULT true,
    CONSTRAINT ck_credit_product_terms CHECK (min_term_months <= max_term_months),
    CONSTRAINT ck_credit_product_amounts CHECK (min_amount <= max_amount)
);

-- -----------------------------------------------------------------------------
-- Клієнти і рахунки
-- -----------------------------------------------------------------------------
CREATE TABLE client (
    id           serial        PRIMARY KEY,
    client_type  char(1)       NOT NULL CHECK (client_type IN ('P', 'L')),
    full_name    varchar(200)  NOT NULL,
    tax_id       varchar(10)   NOT NULL,
    document     varchar(30)   NOT NULL DEFAULT '',
    birth_date   date,
    phone        varchar(20)   NOT NULL DEFAULT '',
    email        varchar(100)  NOT NULL DEFAULT '',
    address      varchar(250)  NOT NULL DEFAULT '',
    status       varchar(10)   NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'blocked')),
    created_at   timestamptz   NOT NULL DEFAULT now(),
    updated_at   timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT ux_client_tax_id UNIQUE (tax_id),
    CONSTRAINT ck_client_tax_id CHECK (
        (client_type = 'P' AND tax_id ~ '^[0-9]{10}$') OR (client_type = 'L' AND tax_id ~ '^[0-9]{8}$'))
);
CREATE INDEX ix_client_full_name ON client (lower(full_name));
COMMENT ON TABLE client IS 'Клієнти банку: P — фізична особа (РНОКПП), L — юридична особа (ЄДРПОУ)';

CREATE TABLE account (
    id             serial        PRIMARY KEY,
    number         char(29)      NOT NULL UNIQUE,
    client_id      integer       NOT NULL REFERENCES client (id),
    currency_code  char(3)       NOT NULL REFERENCES currency (code),
    account_type   varchar(10)   NOT NULL CHECK (account_type IN ('current', 'deposit', 'credit')),
    balance        numeric(18,2) NOT NULL DEFAULT 0 CHECK (balance >= 0),
    status         varchar(10)   NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'closed')),
    opened_at      date          NOT NULL DEFAULT current_date,
    closed_at      date,
    updated_at     timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT ck_account_number CHECK (number ~ '^UA[0-9]{27}$'),
    CONSTRAINT ck_account_closed CHECK ((status = 'closed') = (closed_at IS NOT NULL))
);
CREATE INDEX ix_account_client ON account (client_id);
COMMENT ON TABLE account IS 'Рахунки клієнтів. Для кредитного рахунку balance — залишок заборгованості';
COMMENT ON COLUMN account.number IS 'Номер рахунку у форматі IBAN (UA + 2 контрольні цифри + 6 цифр коду банку + 19 цифр)';

CREATE SEQUENCE account_number_seq START 1001;

-- -----------------------------------------------------------------------------
-- Операції (незмінний журнал проводок)
-- -----------------------------------------------------------------------------
CREATE TABLE operation (
    id                bigserial     PRIMARY KEY,
    op_time           timestamptz   NOT NULL DEFAULT now(),
    op_type           varchar(20)   NOT NULL CHECK (op_type IN (
                          'CASH_IN', 'CASH_OUT', 'TRANSFER', 'DEPOSIT_OPEN', 'DEPOSIT_INTEREST',
                          'DEPOSIT_CLOSE', 'CREDIT_ISSUE', 'CREDIT_PAYMENT')),
    from_account_id   integer       REFERENCES account (id),
    to_account_id     integer       REFERENCES account (id),
    amount            numeric(18,2) NOT NULL CHECK (amount > 0),
    currency_code     char(3)       NOT NULL REFERENCES currency (code),
    amount_to         numeric(18,2) CHECK (amount_to > 0),
    currency_to       char(3)       REFERENCES currency (code),
    rate              numeric(14,6),
    description       varchar(250)  NOT NULL DEFAULT '',
    user_id           integer       REFERENCES app_user (id),
    CONSTRAINT ck_operation_accounts CHECK (from_account_id IS NOT NULL OR to_account_id IS NOT NULL)
);
CREATE INDEX ix_operation_time ON operation (op_time);
CREATE INDEX ix_operation_from ON operation (from_account_id);
CREATE INDEX ix_operation_to ON operation (to_account_id);
COMMENT ON TABLE operation IS 'Фінансові операції. Записи не змінюються і не видаляються (тригер)';

-- -----------------------------------------------------------------------------
-- Депозити і кредити
-- -----------------------------------------------------------------------------
CREATE TABLE deposit (
    id                 serial        PRIMARY KEY,
    client_id          integer       NOT NULL REFERENCES client (id),
    product_id         integer       NOT NULL REFERENCES deposit_product (id),
    account_id         integer       NOT NULL UNIQUE REFERENCES account (id),
    payout_account_id  integer       NOT NULL REFERENCES account (id),
    principal          numeric(18,2) NOT NULL CHECK (principal > 0),
    annual_rate        numeric(5,2)  NOT NULL,
    capitalization     boolean       NOT NULL,
    start_date         date          NOT NULL,
    end_date           date          NOT NULL,
    last_accrual_date  date          NOT NULL,
    interest_paid      numeric(18,2) NOT NULL DEFAULT 0,
    status             varchar(15)   NOT NULL DEFAULT 'active'
                           CHECK (status IN ('active', 'closed', 'closed_early')),
    closed_at          date,
    CONSTRAINT ck_deposit_dates CHECK (end_date > start_date)
);
CREATE INDEX ix_deposit_client ON deposit (client_id);

CREATE TABLE credit (
    id                  serial        PRIMARY KEY,
    client_id           integer       NOT NULL REFERENCES client (id),
    product_id          integer       NOT NULL REFERENCES credit_product (id),
    account_id          integer       NOT NULL UNIQUE REFERENCES account (id),
    current_account_id  integer       NOT NULL REFERENCES account (id),
    amount              numeric(18,2) NOT NULL CHECK (amount > 0),
    annual_rate         numeric(5,2)  NOT NULL,
    term_months         integer       NOT NULL CHECK (term_months > 0),
    monthly_payment     numeric(18,2) NOT NULL,
    issue_date          date          NOT NULL,
    status              varchar(10)   NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'overdue', 'repaid')),
    closed_at           date
);
CREATE INDEX ix_credit_client ON credit (client_id);

CREATE TABLE credit_schedule (
    credit_id       integer       NOT NULL REFERENCES credit (id) ON DELETE CASCADE,
    payment_no      integer       NOT NULL,
    due_date        date          NOT NULL,
    payment         numeric(18,2) NOT NULL,
    principal_part  numeric(18,2) NOT NULL,
    interest_part   numeric(18,2) NOT NULL,
    balance_after   numeric(18,2) NOT NULL,
    paid_at         timestamptz,
    PRIMARY KEY (credit_id, payment_no)
);
COMMENT ON TABLE credit_schedule IS 'Графік погашення кредиту (ануїтетні платежі)';

-- -----------------------------------------------------------------------------
-- Журнал системи
-- -----------------------------------------------------------------------------
CREATE TABLE audit_log (
    id           bigserial    PRIMARY KEY,
    event_time   timestamptz  NOT NULL DEFAULT now(),
    user_id      integer,
    user_login   varchar(50)  NOT NULL DEFAULT '',
    event_type   varchar(15)  NOT NULL CHECK (event_type IN (
                     'LOGIN', 'LOGOUT', 'LOGIN_FAILED', 'INSERT', 'UPDATE', 'DELETE', 'VIEW', 'REPORT', 'ACTION')),
    table_name   varchar(50)  NOT NULL DEFAULT '',
    record_id    varchar(50)  NOT NULL DEFAULT '',
    summary      varchar(300) NOT NULL DEFAULT '',
    details      jsonb,
    ip_address   varchar(45)  NOT NULL DEFAULT ''
);
CREATE INDEX ix_audit_log_time ON audit_log (event_time DESC);
CREATE INDEX ix_audit_log_user ON audit_log (user_id);
COMMENT ON TABLE audit_log IS 'Журнал роботи системи: вхід/вихід, зміни даних (тригери), перегляд таблиць, звіти';

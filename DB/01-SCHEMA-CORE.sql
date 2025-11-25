-- ==============================================================================
-- PASSO 1.0 - LIMPEZA E CRIAÇÃO DO SCHEMA (IDEMPOTÊNCIA GARANTIDA)
-- ==============================================================================
DROP SCHEMA IF EXISTS CORE CASCADE;
CREATE SCHEMA CORE;

-- ==============================================================================
-- PASSO 2.0 - TABELAS DE REFERÊNCIA (LOOKUP TABLES)
-- ==============================================================================


-- ==============================================================================
-- PASSO 2.1 - TABELA: ACTION_LOG_TYPES (TIPOS DE AÇÃO PARA O LOG)
-- ==============================================================================
CREATE TABLE CORE.ACTION_LOG_TYPES (
    ACTION_LOG_TYPE_ID SERIAL NOT NULL,
    ACTION_LOG_TYPES_NOME VARCHAR(50) NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_ACTION_LOG_TYPE PRIMARY KEY (ACTION_LOG_TYPE_ID)
);

-- ==============================================================================
-- PASSO 2.2 - TABELA: USER (TABELA DA CLASSE ABSTRATA USUÁRIOS DO SISTEMA)
-- ==============================================================================
CREATE TABLE CORE.USER (
    USER_ID            UUID PRIMARY KEY,
    USER_NAME          VARCHAR(254) NOT NULL,
    USER_NIF           CHAR(9) NOT NULL,
    USER_EMAIL         VARCHAR(254) NOT NULL,
    USER_PASSWORD_HASH TEXT NOT NULL,
    USER_IS_ACTIVE     BOOLEAN NOT NULL DEFAULT TRUE,

    -- PRIMARY KEY
    CONSTRAINT PK_USER PRIMARY KEY (USER_ID),

    -- UNIQUE
    CONSTRAINT UK_USER_01_NIF UNIQUE (USER_NIF),
    CONSTRAINT UK_USER_02_EMAIL UNIQUE (USER_EMAIL)
);


-- ==============================================================================
-- PASSO 2.3 - TABELA DE LOG
-- ==============================================================================
CREATE TABLE CORE.PERSON_LOG (
    PERSON_LOG_ID BIGSERIAL NOT NULL,
    PERSON_LOG_OLD_VALUE JSONB NOT NULL,
    PERSON_LOG_NEW_VALUE JSONB NOT NULL,
    PERSON_LOG_UPDATED_AT TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PERSON_ID UUID NOT NULL,
    PERSON_LOG_UPDATED_BY UUID NOT NULL,
    ACTION_LOG_TYPE_ID INT NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_PERSON_LOG PRIMARY KEY (PERSON_LOG_ID),

    -- FOREIGN KEYS
    CONSTRAINT FK_PERSON_LOG_01 FOREIGN KEY (PERSON_ID)
        REFERENCES CORE.PERSON (PERSON_ID),
    CONSTRAINT FK_PERSON_LOG_02 FOREIGN KEY (ACTION_LOG_TYPE_ID)
        REFERENCES CORE.ACTION_LOG_TYPES (ACTION_LOG_TYPE_ID),
    CONSTRAINT FK_PERSON_LOG_03 FOREIGN KEY (PERSON_LOG_UPDATED_BY)
        REFERENCES CORE.PERSON (PERSON_ID)
);

-- ==============================================================================
-- PASSO 2.3.1 - ÍNDICES DAS FKs DA TABELA  PERSON_LOG
-- ==============================================================================
CREATE INDEX IDX_PERSON_LOG_01 ON CORE.PERSON_LOG(PERSON_ID);
CREATE INDEX IDX_PERSON_LOG_02 ON CORE.PERSON_LOG(ACTION_LOG_TYPE_ID);
CREATE INDEX IDX_PERSON_LOG_03 ON CORE.PERSON_LOG(PERSON_LOG_UPDATED_BY);

-- ==============================================================================
-- PASSO 2.4 - TABELA DE PHONE
-- ==============================================================================
CREATE TABLE CORE.PHONE(
    PHONE_ID SERIAL NOT NULL,
    PHONE_COUNTRY_CODE VARCHAR(5) NOT NULL DEFAULT '+351',
    PHONE_NUMBER VARCHAR(15) NOT NULL,
    PERSON_ID UUID NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_PHONE PRIMARY KEY (PHONE_ID),

    -- UNIQUE
    -- NOT REQUIRED DUE TO THE RELATIONSHIP IS 1:1F

    -- FOREIGN KEY
    CONSTRAINT FK_PHONE_01 FOREIGN KEY (PERSON_ID)
        REFERENCES CORE.PERSON (PERSON_ID)
);

-- ==============================================================================
-- PASSO 2.4.1 - ÍNDICES DAS FKs DA TABELA  PHONE
-- ==============================================================================
CREATE INDEX IDX_PHONE_01 ON CORE.PHONE(PERSON_ID);


-- ==============================================================================
-- PASSO 2.5 - TABELA DE ADDRESS
-- ==============================================================================
CREATE TABLE CORE.ADDRESS(
    ADDRESS_ID SERIAL NOT NULL,
    ADDRESS_ZIPCODE VARCHAR(8) NOT NULL,
    ADDRESS_COUNTRY CHAR(3) NOT NULL DEFAULT 'PRT',
    ADDRESS_STATE VARCHAR(100) NOT NULL,
    ADDRESS_CITY VARCHAR(100) NOT NULL,
    ADDRESS_SUB_LOCALITY VARCHAR(100), -- FREGUESIA
    ADDRESS_STREET VARCHAR(150) NOT NULL,
    ADDRESS_NUMBER VARCHAR(50) NOT NULL,
    ADDRESS_COMPLEMENT VARCHAR(50),
    PERSON_ID UUID NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_ADDRESS PRIMARY KEY (ADDRESS_ID),

    -- UNIQUE
    -- NOT REQUIRED DUE TO THE RELATIONSHIP IS 1:1

    -- FOREIGN KEY
    CONSTRAINT FK_ADDRESS_01 FOREIGN KEY (PERSON_ID)
        REFERENCES CORE.PERSON (PERSON_ID)
);

-- ==============================================================================
-- PASSO 2.5.1 - ÍNDICES DAS FKs DA TABELA  PHONE
-- ==============================================================================
CREATE INDEX IDX_ADDRESS_01 ON CORE.ADDRESS(PERSON_ID);

-- ==============================================================================
-- PASSO 2.6 - TABELA DE ADDRESS
-- ==============================================================================

CREATE TABLE CORE.CLIENT(
    CLIENT_ID UUID NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_CLIENT PRIMARY KEY (CLIENT_ID),

    -- UNIQUE
    -- NOT REQUIRED

    -- FOREIGN KEY
    CONSTRAINT FK_CLIENT_01 FOREIGN KEY (CLIENT_ID)
        REFERENCES CORE.PERSON (PERSON_ID)
);

-- ==============================================================================
-- PASSO 2.6.1 - ÍNDICES DAS FKs DA TABELA CLIENT
-- ==============================================================================
CREATE INDEX IDX_CLIENT_01 ON CORE.CLIENT(CLIENT_ID);
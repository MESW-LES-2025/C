--- ==============================================================================
--- PASSO 1.0 - LIMPEZA E CRIAÇÃO DO SCHEMA (IDEMPOTÊNCIA GARANTIDA)
--- ==============================================================================

DROP SCHEMA IF EXISTS CORE CASCADE;
CREATE SCHEMA CORE;

--- ==============================================================================
--- PASSO 2.1 - TABELA: ACTION_LOG_TYPES (TIPOS DE AÇÃO PARA O LOG)
--- ==============================================================================
CREATE TABLE CORE.ACTION_LOG_TYPE (
    ACTION_LOG_TYPE_ID SERIAL NOT NULL,
    ACTION_LOG_TYPE_NAME VARCHAR(100) NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_ACTION_LOG_TYPE PRIMARY KEY (ACTION_LOG_TYPE_ID),
    -- UNIQUE CONSTRAINT
    CONSTRAINT UK_ACTION_LOG_TYPE_01 UNIQUE (ACTION_LOG_TYPE_NAME)
);

--- ==============================================================================
--- PASSO 2.2 - TABELA: USER (TABELA DA CLASSE ABSTRATA USUÁRIOS DO SISTEMA)
--- ==============================================================================
CREATE TABLE CORE.USER (
    USER_ID UUID NOT NULL,
    USER_NAME VARCHAR(254) NOT NULL,
    USER_NIF CHAR(9) NOT NULL,
    USER_EMAIL VARCHAR(254) NOT NULL,
    USER_PASSWORD_HASH TEXT NOT NULL,
    USER_IS_ACTIVE BOOLEAN NOT NULL DEFAULT TRUE,

    -- PRIMARY KEY
    CONSTRAINT PK_USER PRIMARY KEY (USER_ID),
    
    -- UNIQUE CONSTRAINT
    CONSTRAINT UK_USER_01_NIF UNIQUE (USER_NIF),
    CONSTRAINT UK_USER_02_EMAIL UNIQUE (USER_EMAIL)
);

--- ==============================================================================
--- PASSO 2.3 - TABELA DE LOG
--- ==============================================================================
CREATE TABLE CORE.USER_LOG (
    USER_LOG_ID UUID NOT NULL,
    AFFECTED_USER_ID UUID,
    UPDATED_BY_ID UUID,
    USER_LOG_UPDATED_AT TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    USER_LOG_OLD_VALUE JSONB NOT NULL,
    USER_LOG_NEW_VALUE JSONB NOT NULL,
    ACTION_LOG_TYPE_ID INTEGER,

    -- PRIMARY KEY
    CONSTRAINT PK_USER_LOG PRIMARY KEY (USER_LOG_ID),
    
    -- FOREIGN KEY
    CONSTRAINT FK_USER_LOG_AFFECTED_USER_01 FOREIGN KEY (AFFECTED_USER_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE SET NULL,
    CONSTRAINT FK_USER_LOG_UPDATED_BY_01 FOREIGN KEY (UPDATED_BY_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE SET NULL
);


-- ==============================================================================
-- PASSO 2.3.1 - ÍNDICES DAS FKs DA TABELA  USER_LOG
-- ==============================================================================
CREATE INDEX IDX_USER_LOG_01 ON CORE.USER_LOG(AFFECTED_USER_ID);
CREATE INDEX IDX_USER_LOG_02 ON CORE.USER_LOG(UPDATED_BY_ID);
CREATE INDEX IDX_USER_LOG_03 ON CORE.USER_LOG(ACTION_LOG_TYPE_ID);


--- ==============================================================================
--- PASSO 2.4 - TABELA DE PHONE
--- ==============================================================================
CREATE TABLE CORE.PHONE (
    PHONE_ID UUID NOT NULL,
    FK_USER_ID UUID NOT NULL,
    PHONE_COUNTRY_CODE SMALLINT NOT NULL DEFAULT 351,
    PHONE_NUMBER VARCHAR(20) NOT NULL,
    PHONE_IS_MAIN BOOLEAN NOT NULL DEFAULT TRUE,

    -- PRIMARY KEY
    CONSTRAINT PK_PHONE PRIMARY KEY (PHONE_ID),
    
    -- UNIQUE CONSTRAINT
    CONSTRAINT UK_PHONE_01_USER_NUMBER UNIQUE (FK_USER_ID, PHONE_COUNTRY_CODE, PHONE_NUMBER),
    
    -- FOREIGN KEY
    CONSTRAINT FK_PHONE_USER_01 FOREIGN KEY (FK_USER_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE CASCADE
);

-- ==============================================================================
-- PASSO 2.4.1 - ÍNDICES DAS FKs DA TABELA  PHONE
-- ==============================================================================
CREATE INDEX IDX_PHONE_01 ON CORE.PHONE(FK_USER_ID);

--- ==============================================================================
--- PASSO 2.5 - TABELA: CLIENT
--- ==============================================================================
CREATE TABLE CORE.CLIENT (
    CLIENT_ID UUID NOT NULL,
    CLIENT_ADDRESS VARCHAR(500) NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_CLIENT PRIMARY KEY (CLIENT_ID),

    -- FOREIGN KEY
    CONSTRAINT FK_CLIENT_USER_01 FOREIGN KEY (CLIENT_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE CASCADE
);

--- ==============================================================================
--- PASSO 2.5.1 - ÍNDICES DAS FKs DA TABELA  CLIENT
--- ==============================================================================
CREATE INDEX IDX_CLIENT_01 ON CORE.CLIENT(CLIENT_ID);

--- ==============================================================================
--- PASSO 2.6 - TABELA: LAWYER
--- ==============================================================================
CREATE TABLE CORE.LAWYER (
    LAWYER_ID UUID NOT NULL,
    LAWYER_PROFESSIONAL_REGISTER VARCHAR(20) NOT NULL,

    -- PRIMARY KEY
    CONSTRAINT PK_LAWYER PRIMARY KEY (LAWYER_ID),

    -- UNIQUE CONSTRAINT
    CONSTRAINT UK_LAWYER_01_REGISTER UNIQUE (LAWYER_PROFESSIONAL_REGISTER),

    -- FOREIGN KEY
    CONSTRAINT FK_LAWYER_USER_01 FOREIGN KEY (LAWYER_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE CASCADE
);  

--- ============================================================================== 
--- PASSO 2.6.1 - ÍNDICES DAS FKs DA TABELA  LAWYER
--- ============================================================================== 
CREATE INDEX IDX_LAWYER_01 ON CORE.LAWYER(LAWYER_ID);


--- ============================================================================== 
--- PASSO 2.7 - TABELA: ADMIN
--- ============================================================================== 
CREATE TABLE CORE.ADMIN (
    ADMIN_ID UUID NOT NULL,
    ADMIN_STARTED_AT TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- PRIMARY KEY
    CONSTRAINT PK_ADMIN PRIMARY KEY (ADMIN_ID),

    -- FOREIGN KEY
    CONSTRAINT FK_ADMIN_USER_01 FOREIGN KEY (ADMIN_ID) 
        REFERENCES CORE.USER (USER_ID) ON DELETE CASCADE
);

-- ============================================================================== 
-- PASSO 2.7.1 - ÍNDICES DAS FKs DA TABELA  ADMIN
-- ============================================================================== 
CREATE INDEX IDX_ADMIN_01 ON CORE.ADMIN(ADMIN_ID);  
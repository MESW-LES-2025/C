--- ==============================================================================
--- PASSO 1.0 - LIMPEZA E CRIAÇÃO DO SCHEMA (IDEMPOTÊNCIA GARANTIDA)
--- ==============================================================================
DO $$
BEGIN

--- ==============================================================================
--- PASSO 1.1 - VERIFICAÇÃO DE SEGURANÇA: SE EXISTIR BACKUP DO SCHEMA, ABORTAREMOS A MISSÃO
--- ==============================================================================
    IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'communication_old') THEN
        RAISE EXCEPTION 'Atenção: O schema "communication_old" já existe. O script foi interrompido para evitar perda de backups anteriores. Decida o que fazer com o backup antigo antes de rodar este script novamente.';
    END IF;

--- ==============================================================================
--- PASSO 1.2 - SE O SCHEMA COMMUNICATION EXISTIR, O RENOMEAREMOS COM O SUFIXO _OLD
--- ==============================================================================
    IF EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'communication') THEN
        ALTER SCHEMA communication RENAME TO communication_old;
        RAISE NOTICE 'Schema "communication" foi renomeado para "communication_old".';
    ELSE
        RAISE NOTICE 'Schema "communication" não existia. Criando um novo do zero.';
    END IF;
END $$;

--- ==============================================================================
--- PASSO 1.3 - CRIA O NOVO SCHEMA COMMUNICATION
--- ==============================================================================
-- 3. Cria o novo schema communication
CREATE SCHEMA communication;


--- ==============================================================================
--- PASSO 2.0 - CRIAÇÃO DE TABELAS
--- ==============================================================================
--- ==============================================================================
--- PASSO 2.1 - TABELA: MESSAGE (MENSAGENS ENTRE CLIENTE E ADVOGADO)
--- ==============================================================================
CREATE TABLE COMMUNICATION.MESSAGE (
    MESSAGE_ID           SERIAL NOT NULL,
    MESSAGE_SENDER_ID    UUID NOT NULL,
    MESSAGE_RECIPIENT_ID UUID NOT NULL,
    PROCESS_ID           UUID NOT NULL,
    MESSAGE_SUBJECT      VARCHAR(254) NOT NULL,
    MESSAGE_BODY         TEXT NOT NULL,
    MESSAGE_CREATED_AT   TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    MESSAGE_READ_AT      TIMESTAMP NULL,

    --- PRIMARY KEY
    CONSTRAINT PK_MESSAGE PRIMARY KEY (MESSAGE_ID),

    --- FOREIGN KEYS
    CONSTRAINT FK_MESSAGE_SENDER
        FOREIGN KEY (MESSAGE_SENDER_ID)
        REFERENCES CORE.USER (USER_ID),

    CONSTRAINT FK_MESSAGE_RECIPIENT
        FOREIGN KEY (MESSAGE_RECIPIENT_ID)
        REFERENCES CORE.USER (USER_ID),

    CONSTRAINT FK_MESSAGE_PROCESS
        FOREIGN KEY (PROCESS_ID)
        REFERENCES LEGAL.PROCESS (PROCESS_ID)
);

CREATE INDEX IDX_MESSAGE_01 ON COMMUNICATION.MESSAGE (MESSAGE_SENDER_ID);
CREATE INDEX IDX_MESSAGE_02 ON COMMUNICATION.MESSAGE (MESSAGE_RECIPIENT_ID);
CREATE INDEX IDX_MESSAGE_03 ON COMMUNICATION.MESSAGE (PROCESS_ID);
CREATE INDEX IDX_MESSAGE_04 ON COMMUNICATION.MESSAGE (PROCESS_ID, MESSAGE_CREATED_AT);

--- ==============================================================================
--- PASSO 2.2 - TABELA: MESSAGE_LOG (AUDITORIA DO CICLO DE VIDA DAS MENSAGENS)
--- ==============================================================================
CREATE TABLE COMMUNICATION.MESSAGE_LOG (
    MESSAGE_LOG_ID      BIGSERIAL NOT NULL,
    MESSAGE_ID          INT NOT NULL,
    UPDATED_BY_ID       UUID NOT NULL,
    ACTION_LOG_TYPE_ID  INT NOT NULL,
    UPDATED_AT          TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    NEW_VALUE           JSONB NOT NULL,
    OLD_VALUE           JSONB NOT NULL,

    --- PRIMARY KEY
    CONSTRAINT PK_MESSAGE_LOG PRIMARY KEY (MESSAGE_LOG_ID),

    --- FOREIGN KEYS
    CONSTRAINT FK_MESSAGE_LOG_MESSAGE
        FOREIGN KEY (MESSAGE_ID)
        REFERENCES COMMUNICATION.MESSAGE (MESSAGE_ID),

    CONSTRAINT FK_MESSAGE_LOG_USER
        FOREIGN KEY (UPDATED_BY_ID)
        REFERENCES CORE.USER (USER_ID),

    CONSTRAINT FK_MESSAGE_LOG_ACTION_TYPE
        FOREIGN KEY (ACTION_LOG_TYPE_ID)
        REFERENCES CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID)
);

CREATE INDEX IDX_MESSAGE_LOG_01 ON COMMUNICATION.MESSAGE_LOG (MESSAGE_ID);
CREATE INDEX IDX_MESSAGE_LOG_02 ON COMMUNICATION.MESSAGE_LOG (UPDATED_BY_ID);
CREATE INDEX IDX_MESSAGE_LOG_03 ON COMMUNICATION.MESSAGE_LOG (ACTION_LOG_TYPE_ID);

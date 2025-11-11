/*
================================================================
SCRIPT 6: SEED TRASH DATA FOR TESTING
(Adaptado ao schema CORE/LEGAL/COMMS)
================================================================
*/

-- Usar um DO block para declarar variáveis e gerir UUIDs
DO $$
DECLARE
    -- User IDs
    client1_id UUID;
    client2_id UUID;
    lawyer1_id UUID;
    lawyer2_id UUID;
    admin1_id UUID;
    
    -- Workflow IDs
    p_type_civil UUID;
    p_phase_initial UUID;
    p_phase_summons UUID;
    p_status_open UUID;
    p_status_pending UUID;
    tp_civil_initial UUID;
    tp_civil_summons UUID;
    state_civil_open UUID;
    state_civil_pending UUID;

    -- Entity IDs
    process1_id UUID;
    process2_id UUID;
    chat1_id UUID;
    chat2_id UUID;
    
    -- Log Type ID
    log_type_create UUID;
    log_type_update UUID;

BEGIN
    -- Vamos assumir que a extensão uuid-ossp está ativa para gen_random_uuid()
    -- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

    RAISE NOTICE '=== 1. A Criar Catálogos de Logs ===';
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'USER_CREATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'USER_UPDATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_update;

    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'USER_DELETED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'CLIENT_CREATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'CLIENT_UPDATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_update;

    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'CLIENT_DELETED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_delete; -- <-- Fixed

    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'LAWYER_CREATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'LAWYER_UPDATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_update;

    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'LAWYER_DELETED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'ADMIN_CREATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'ADMIN_UPDATED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_update;

    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES (gen_random_uuid(), 'ADMIN_DELETED') RETURNING ACTION_LOG_TYPE_ID INTO log_type_create;
    

    RAISE NOTICE '=== 2. A Criar Users & Roles ===';

    -- Client 1: Alice Smith
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'alice.smith@example.com', '...hash...', 'Alice Smith', '111111111', TRUE)
    RETURNING USER_ID INTO client1_id;
    
    INSERT INTO CORE.CLIENT (CLIENT_ID, CLIENT_ADDRESS)
    VALUES (client1_id, '123 Main St, Anytown');

    INSERT INTO CORE.PHONE (PHONE_ID, FK_USER_ID, PHONE_COUNTRY_CODE, PHONE_NUMBER, PHONE_IS_MAIN)
    VALUES (gen_random_uuid(), client1_id, 351, '911111111', TRUE);

    -- Client 2: Bob Jones (Inactive)
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'bob.jones@example.com', '...hash...', 'Bob Jones', '222222222', FALSE)
    RETURNING USER_ID INTO client2_id;
    
    INSERT INTO CORE.CLIENT (CLIENT_ID, CLIENT_ADDRESS)
    VALUES (client2_id, '456 Oak Ave, Othertown');
    
    INSERT INTO CORE.PHONE (PHONE_ID, FK_USER_ID, PHONE_COUNTRY_CODE, PHONE_NUMBER, PHONE_IS_MAIN)
    VALUES (gen_random_uuid(), client2_id, 351, '922222222', TRUE);

    -- Lawyer 1: Saul Goodman
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'saul.goodman@example.com', '...hash...', 'Saul Goodman', '333333333', TRUE)
    RETURNING USER_ID INTO lawyer1_id;

    INSERT INTO CORE.LAWYER (LAWYER_ID, LAWYER_PROFESSIONAL_REGISTER)
    VALUES (lawyer1_id, 'PN-12345');
    
    INSERT INTO CORE.PHONE (PHONE_ID, FK_USER_ID, PHONE_COUNTRY_CODE, PHONE_NUMBER, PHONE_IS_MAIN)
    VALUES (gen_random_uuid(), lawyer1_id, 351, '933333333', TRUE);

    -- Lawyer 2: Kim Wexler
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'kim.wexler@example.com', '...hash...', 'Kim Wexler', '444444444', TRUE)
    RETURNING USER_ID INTO lawyer2_id;

    INSERT INTO CORE.LAWYER (LAWYER_ID, LAWYER_PROFESSIONAL_REGISTER)
    VALUES (lawyer2_id, 'PN-67890');
    
    INSERT INTO CORE.PHONE (PHONE_ID, FK_USER_ID, PHONE_COUNTRY_CODE, PHONE_NUMBER, PHONE_IS_MAIN)
    VALUES (gen_random_uuid(), lawyer2_id, 351, '944444444', TRUE);
    
    -- Admin 1: Admin User
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'admin@consilium.app', '...hash...', 'Admin User', '999999999', TRUE)
    RETURNING USER_ID INTO admin1_id;
    
    INSERT INTO CORE.ADMIN (ADMIN_ID, ADMIN_STARTED_AT)
    VALUES (admin1_id, '2023-01-01');

    RAISE NOTICE 'Criados 2 Clientes, 2 Advogados, e 1 Admin (e 4 telefones)';

    -- === 3. CRIAR WORKFLOW DE PROCESSOS (Catálogos) ===
    -- Isto é obrigatório antes de se poder criar um LEGAL.PROCESS
    
    RAISE NOTICE '=== 3. A Criar Catálogos de Workflow ===';

    -- Tipos de Processo
    INSERT INTO LEGAL.PROCESS_TYPE (PROCESS_TYPE_ID, PROCESS_TYPE_NAME)
    VALUES (gen_random_uuid(), 'Ação Civil') RETURNING PROCESS_TYPE_ID INTO p_type_civil;

    -- Fases
    INSERT INTO LEGAL.PROCESS_PHASE (PROCESS_PHASE_ID, PROCESS_PHASE_NAME)
    VALUES (gen_random_uuid(), 'Petição Inicial') RETURNING PROCESS_PHASE_ID INTO p_phase_initial;
    
    INSERT INTO LEGAL.PROCESS_PHASE (PROCESS_PHASE_ID, PROCESS_PHASE_NAME)
    VALUES (gen_random_uuid(), 'Citação e Defesa') RETURNING PROCESS_PHASE_ID INTO p_phase_summons;

    -- Status
    INSERT INTO LEGAL.PROCESS_STATUS (PROCESS_STATUS_ID, PROCESS_STATUS_NAME)
    VALUES (gen_random_uuid(), 'Aberto') RETURNING PROCESS_STATUS_ID INTO p_status_open;
    
    INSERT INTO LEGAL.PROCESS_STATUS (PROCESS_STATUS_ID, PROCESS_STATUS_NAME)
    VALUES (gen_random_uuid(), 'Pendente') RETURNING PROCESS_STATUS_ID INTO p_status_pending;

    -- Ligar Tipos a Fases (N:N)
    INSERT INTO LEGAL.PROCESS_TYPE_PHASE (PROCESS_TYPE_PHASE_ID, PROCESS_TYPE_ID, PROCESS_PHASE_ID, PROCESS_TYPE_PHASE_ORDER_INDEX)
    VALUES (gen_random_uuid(), p_type_civil, p_phase_initial, 1) RETURNING PROCESS_TYPE_PHASE_ID INTO tp_civil_initial;
    
    INSERT INTO LEGAL.PROCESS_TYPE_PHASE (PROCESS_TYPE_PHASE_ID, PROCESS_TYPE_ID, PROCESS_PHASE_ID, PROCESS_TYPE_PHASE_ORDER_INDEX)
    VALUES (gen_random_uuid(), p_type_civil, p_phase_summons, 2) RETURNING PROCESS_TYPE_PHASE_ID INTO tp_civil_summons;

    -- Criar os "Estados" finais (Fase + Status)
    INSERT INTO LEGAL.PROCESS_STATE (PROCESS_STATE_ID, PROCESS_TYPE_PHASE_ID, PROCESS_STATUS_ID)
    VALUES (gen_random_uuid(), tp_civil_initial, p_status_open) RETURNING PROCESS_STATE_ID INTO state_civil_open;
    
    INSERT INTO LEGAL.PROCESS_STATE (PROCESS_STATE_ID, PROCESS_TYPE_PHASE_ID, PROCESS_STATUS_ID)
    VALUES (gen_random_uuid(), tp_civil_summons, p_status_pending) RETURNING PROCESS_STATE_ID INTO state_civil_pending;
    
    RAISE NOTICE 'Catálogos de Workflow populados.';


    -- === 4. CRIAR ENTIDADES RELACIONADAS ===

    RAISE NOTICE '=== 4. A Criar Entidades (Processos, Docs, Chats) ===';

    -- Processo (para Alice + Saul)
    INSERT INTO LEGAL.PROCESS (PROCESS_ID, CLIENT_ID, LAWYER_ID, PROCESS_NUMBER, PROCESS_STATE_ID, PROCESS_CREATED_BY, PROCESS_DSC)
    VALUES (gen_random_uuid(), client1_id, lawyer1_id, 'PROC-2025-001', state_civil_open, admin1_id, 'Civil Case: Smith v. Corp. Initial petition for damages.')
    RETURNING PROCESS_ID INTO process1_id;

    -- Processo (para Bob + Kim)
    INSERT INTO LEGAL.PROCESS (PROCESS_ID, CLIENT_ID, LAWYER_ID, PROCESS_NUMBER, PROCESS_STATE_ID, PROCESS_CREATED_BY, PROCESS_DSC)
    VALUES (gen_random_uuid(), client2_id, lawyer2_id, 'PROC-2025-002', state_civil_pending, admin1_id, 'Real Estate Dispute. Dispute over property lines.')
    RETURNING PROCESS_ID INTO process2_id;

    -- Documento (para o processo de Alice)
    -- O schema novo guarda um *path* (TEXT), não o ficheiro (bytea)
    INSERT INTO LEGAL.DOCUMENT (DOCUMENT_ID, PROCESS_ID, DOCUMENT_CREATED_BY, DOCUMENT_NAME, DOCUMENT_TYPE, DOCUMENT_FILE_PATH)
    VALUES (gen_random_uuid(), process1_id, client1_id, 'Initial_Petition.pdf', 'application/pdf', '/uploads/proc-2025-001/Initial_Petition.pdf');
    
    INSERT INTO LEGAL.DOCUMENT (DOCUMENT_ID, PROCESS_ID, DOCUMENT_CREATED_BY, DOCUMENT_NAME, DOCUMENT_TYPE, DOCUMENT_FILE_PATH)
    VALUES (gen_random_uuid(), process1_id, client1_id, 'Evidence_A.jpg', 'image/jpeg', '/uploads/proc-2025-001/Evidence_A.jpg');

    RAISE NOTICE 'Criados 2 Processos e 2 Documentos';

    -- Chat (entre Alice + Saul, ligado ao Processo 1)
    -- O schema novo obriga a ligar o Chat a um Processo
    INSERT INTO COMMS.CHAT (CHAT_ID, LAWYER_ID, CLIENT_ID, PROCESS_ID)
    VALUES (gen_random_uuid(), lawyer1_id, client1_id, process1_id)
    RETURNING CHAT_ID INTO chat1_id;

    -- Chat (entre Bob + Kim, ligado ao Processo 2)
    INSERT INTO COMMS.CHAT (CHAT_ID, LAWYER_ID, CLIENT_ID, PROCESS_ID)
    VALUES (gen_random_uuid(), lawyer2_id, client2_id, process2_id)
    RETURNING CHAT_ID INTO chat2_id;

    -- Mensagens (para Chat 1)
    INSERT INTO COMMS.MESSAGE (MESSAGE_ID, CHAT_ID, MESSAGE_SENDER, MESSAGE_TEXT, MESSAGE_SENT_AT, MESSAGE_READ)
    VALUES (gen_random_uuid(), chat1_id, lawyer1_id, 'Hi Alice, I have received your documents.', '2025-10-29 10:00:00', TRUE);
    
    INSERT INTO COMMS.MESSAGE (MESSAGE_ID, CHAT_ID, MESSAGE_SENDER, MESSAGE_TEXT, MESSAGE_SENT_AT, MESSAGE_READ)
    VALUES (gen_random_uuid(), chat1_id, client1_id, 'Thanks Saul. When are you free?', '2025-10-29 10:05:00', TRUE);

    INSERT INTO COMMS.MESSAGE (MESSAGE_ID, CHAT_ID, MESSAGE_SENDER, MESSAGE_TEXT, MESSAGE_SENT_AT, MESSAGE_READ)
    VALUES (gen_random_uuid(), chat1_id, lawyer1_id, 'How about next Tuesday?', '2025-10-29 10:06:00', FALSE);

    RAISE NOTICE 'Criados 2 Chats e 3 Mensagens';
    
    -- === 5. SECÇÕES COMENTADAS (Tabelas não existem no schema) ===
    
    RAISE NOTICE 'A ignorar Appointments e Notifications (tabelas não existem no schema CORE/LEGAL/COMMS)';
    
    /*
    -- Appointment (Alice + Saul)
    INSERT INTO "Appointment" (client_ID, lawyer_ID, process_ID, appointment_time)
    VALUES (client1_id, lawyer1_id, process1_id, '2025-11-05 14:00:00')
    RETURNING "ID" INTO appt1_id;

    -- Appointment Note (by Saul)
    INSERT INTO "Appointment_Notes" (appointment_ID, author_ID, note)
    VALUES (appt1_id, lawyer1_id, 'Client confirmed. Will discuss strategy.');
    
    -- Notification (for Bob)
    INSERT INTO "Notification" (recipient_ID, content, is_read)
    VALUES (client2_id, 'Your process has been updated to PENDING.', false);
    */

    -- === 6. CRIAR ENTRADAS DE LOG (Bónus) ===
    
    RAISE NOTICE '=== 6. A Criar Entradas de Log (Bónus) ===';
    
    -- Log da criação da Alice
    INSERT INTO CORE.USER_LOG (USER_LOG_ID, AFFECTED_USER_ID, UPDATED_BY_ID, ACTION_LOG_TYPE_ID, USER_LOG_OLD_VALUE, USER_LOG_NEW_VALUE)
    VALUES (gen_random_uuid(), client1_id, admin1_id, log_type_create, '{}', '{"email": "alice.smith@example.com", "name": "Alice Smith"}');

    -- Log da atualização do Processo 1
    INSERT INTO LEGAL.PROCESS_LOG (PROCESS_LOG_ID, PROCESS_ID, PROCESS_LOG_UPDATED_BY, ACTION_LOG_TYPE_ID, PROCESS_LOG_OLD_VALUE, PROCESS_LOG_NEW_VALUE)
    VALUES (gen_random_uuid(), process1_id, lawyer1_id, log_type_update, '{"status": "NEW"}', '{"status": "Aberto"}');

    RAISE NOTICE 'Script de Seed concluído com sucesso!';

END $$;
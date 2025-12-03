/*
================================================================
SCRIPT: DEFAULT USERS (PROFESSORS)
================================================================
*/

DO $$
DECLARE
    user_advogado_id UUID;
    user_admin_id UUID;
    user_cliente_id UUID;
    password_hash TEXT := 'yIU1mrEn+4gH3CK0DWKtn/WBP3hKXBvl+dQbFMJfyjQ2cXpT9Y9MpQ0vyGU0WJoy';
BEGIN
    RAISE NOTICE '=== Creating Default Professor Users ===';

    -- 1. Professor Advogado
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'professorAdvogado@gmail.com', password_hash, 'Professor Advogado', '100000001', TRUE)
    RETURNING USER_ID INTO user_advogado_id;

    INSERT INTO CORE.LAWYER (LAWYER_ID, LAWYER_PROFESSIONAL_REGISTER)
    VALUES (user_advogado_id, 'PN-PROF-001');

    RAISE NOTICE 'Created Professor Advogado';

    -- 2. Professor Admin
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'professorAdmin@gmail.com', password_hash, 'Professor Admin', '100000002', TRUE)
    RETURNING USER_ID INTO user_admin_id;

    INSERT INTO CORE.ADMIN (ADMIN_ID)
    VALUES (user_admin_id);

    RAISE NOTICE 'Created Professor Admin';

    -- 3. Professor Cliente
    INSERT INTO CORE.USER (USER_ID, USER_EMAIL, USER_PASSWORD_HASH, USER_NAME, USER_NIF, USER_IS_ACTIVE)
    VALUES (gen_random_uuid(), 'professorCliente@gmail.com', password_hash, 'Professor Cliente', '100000003', TRUE)
    RETURNING USER_ID INTO user_cliente_id;

    INSERT INTO CORE.CLIENT (CLIENT_ID, CLIENT_ADDRESS)
    VALUES (user_cliente_id, 'University Campus, Law Department');

    RAISE NOTICE 'Created Professor Cliente';


    RAISE NOTICE '=== Creating Default Action Log Types ===';
    
    INSERT INTO CORE.ACTION_LOG_TYPE (ACTION_LOG_TYPE_ID, ACTION_LOG_TYPE_NAME)
    VALUES
        (1, 'Create'),
        (2, 'Update'),
        (3, 'Delete');

END $$;
-- Seed Data for Legal Practice Management System
-- This file populates the database with sample data for testing and development

-- Insert sample users
-- Password hashes are bcrypt hashes of 'password123' for testing purposes
INSERT INTO Users (id, email, password_hash, user_type) VALUES
    ('a0000000-0000-0000-0000-000000000001', 'admin@consilium.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'admin'),
    ('c0000000-0000-0000-0000-000000000001', 'john.doe@email.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'client'),
    ('c0000000-0000-0000-0000-000000000002', 'maria.silva@email.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'client'),
    ('c0000000-0000-0000-0000-000000000003', 'carlos.santos@email.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'client'),
    ('10000000-0000-0000-0000-000000000001', 'ana.lawyer@consilium.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'lawyer'),
    ('10000000-0000-0000-0000-000000000002', 'pedro.advocate@consilium.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'lawyer'),
    ('10000000-0000-0000-0000-000000000003', 'rita.counsel@consilium.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'lawyer');

-- Insert admin
INSERT INTO Admins (user_id) VALUES
    ('a0000000-0000-0000-0000-000000000001');

-- Insert clients with addresses
INSERT INTO Clients (user_id, address) VALUES
    ('c0000000-0000-0000-0000-000000000001', 'Rua das Flores, 123, Lisboa, Portugal'),
    ('c0000000-0000-0000-0000-000000000002', 'Avenida da Liberdade, 456, Porto, Portugal'),
    ('c0000000-0000-0000-0000-000000000003', 'Praça do Comércio, 789, Coimbra, Portugal');

-- Insert lawyers with descriptions and photos
INSERT INTO Lawyers (user_id, description, photo) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Specialized in civil law with 15 years of experience. Expert in contract disputes and family law.', 'https://i.pravatar.cc/150?img=1'),
    ('10000000-0000-0000-0000-000000000002', 'Criminal defense attorney with focus on white-collar crimes and corporate law. 10 years of practice.', 'https://i.pravatar.cc/150?img=2'),
    ('10000000-0000-0000-0000-000000000003', 'Labor law specialist with extensive experience in workplace disputes and employment contracts.', 'https://i.pravatar.cc/150?img=3');

-- Insert legal processes
INSERT INTO Processes (id, client_id, lawyer_id, description, created_at, steps_count, current_step) VALUES
    ('90000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 
     'Divorce proceedings - Division of assets and custody arrangements', 
     CURRENT_TIMESTAMP - INTERVAL '60 days', 5, 3),
    ('90000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 
     'Contract dispute - Breach of service agreement with vendor', 
     CURRENT_TIMESTAMP - INTERVAL '30 days', 4, 2),
    ('90000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 
     'Wrongful termination case - Seeking compensation and reinstatement', 
     CURRENT_TIMESTAMP - INTERVAL '15 days', 6, 1),
    ('90000000-0000-0000-0000-000000000004', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 
     'Property sale contract review and negotiation', 
     CURRENT_TIMESTAMP - INTERVAL '120 days', 3, 3),
    ('90000000-0000-0000-0000-000000000005', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 
     'Estate planning and will preparation', 
     CURRENT_TIMESTAMP - INTERVAL '5 days', 4, 0);

-- Close one process
UPDATE Processes 
SET closed_at = CURRENT_TIMESTAMP - INTERVAL '10 days'
WHERE id = '90000000-0000-0000-0000-000000000004';

-- Insert documents
INSERT INTO Documents (process_id, file_path, is_read, uploaded_at) VALUES
    ('90000000-0000-0000-0000-000000000001', '/documents/divorce/petition.pdf', true, CURRENT_TIMESTAMP - INTERVAL '55 days'),
    ('90000000-0000-0000-0000-000000000001', '/documents/divorce/asset_list.pdf', true, CURRENT_TIMESTAMP - INTERVAL '50 days'),
    ('90000000-0000-0000-0000-000000000001', '/documents/divorce/custody_proposal.pdf', false, CURRENT_TIMESTAMP - INTERVAL '2 days'),
    ('90000000-0000-0000-0000-000000000002', '/documents/contract/original_agreement.pdf', true, CURRENT_TIMESTAMP - INTERVAL '28 days'),
    ('90000000-0000-0000-0000-000000000002', '/documents/contract/breach_evidence.pdf', true, CURRENT_TIMESTAMP - INTERVAL '20 days'),
    ('90000000-0000-0000-0000-000000000003', '/documents/employment/termination_letter.pdf', true, CURRENT_TIMESTAMP - INTERVAL '14 days'),
    ('90000000-0000-0000-0000-000000000003', '/documents/employment/employment_contract.pdf', true, CURRENT_TIMESTAMP - INTERVAL '13 days'),
    ('90000000-0000-0000-0000-000000000004', '/documents/property/sales_contract.pdf', true, CURRENT_TIMESTAMP - INTERVAL '115 days'),
    ('90000000-0000-0000-0000-000000000005', '/documents/estate/will_draft.pdf', false, CURRENT_TIMESTAMP - INTERVAL '3 days');

-- Insert appointments (scheduled for future dates)
INSERT INTO Appointments (client_id, lawyer_id, process_id, scheduled_at) VALUES
    ('c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000001', 
     CURRENT_TIMESTAMP + INTERVAL '3 days' + INTERVAL '10 hours'),
    ('c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', '90000000-0000-0000-0000-000000000002', 
     CURRENT_TIMESTAMP + INTERVAL '5 days' + INTERVAL '14 hours'),
    ('c0000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', '90000000-0000-0000-0000-000000000003', 
     CURRENT_TIMESTAMP + INTERVAL '7 days' + INTERVAL '9 hours'),
    ('c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000005', 
     CURRENT_TIMESTAMP + INTERVAL '2 days' + INTERVAL '15 hours'),
    ('c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', NULL, 
     CURRENT_TIMESTAMP + INTERVAL '10 days' + INTERVAL '11 hours');

-- Insert notifications
INSERT INTO Notifications (process_id, client_id, content, created_at, is_read) VALUES
    ('90000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 
     'New document uploaded: Custody proposal. Please review at your earliest convenience.', 
     CURRENT_TIMESTAMP - INTERVAL '2 days', false),
    ('90000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 
     'Your appointment has been scheduled for next week. Please confirm your attendance.', 
     CURRENT_TIMESTAMP - INTERVAL '5 days', true),
    ('90000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 
     'Case status update: We have received a response from the opposing party. Review required.', 
     CURRENT_TIMESTAMP - INTERVAL '3 days', false),
    ('90000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', 
     'Initial consultation completed. Next steps have been outlined in your process details.', 
     CURRENT_TIMESTAMP - INTERVAL '14 days', true),
    ('90000000-0000-0000-0000-000000000005', 'c0000000-0000-0000-0000-000000000002', 
     'Will draft is ready for your review. Please check the uploaded document.', 
     CURRENT_TIMESTAMP - INTERVAL '3 days', false),
    ('90000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 
     'Upcoming appointment reminder: Meeting scheduled in 5 days.', 
     CURRENT_TIMESTAMP - INTERVAL '1 day', false);

-- Insert chats
INSERT INTO Chats (id, client_id, lawyer_id) VALUES
    ('ca000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'),
    ('ca000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002'),
    ('ca000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003'),
    ('ca000000-0000-0000-0000-000000000004', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001'),
    ('ca000000-0000-0000-0000-000000000005', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002');

-- Insert messages
INSERT INTO Messages (chat_id, sender_id, receiver_id, content, sent_at) VALUES
    -- Chat 1: John Doe and Ana Lawyer
    ('ca000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 
     'Good morning, I have some questions about the custody arrangements.', 
     CURRENT_TIMESTAMP - INTERVAL '4 hours'),
    ('ca000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 
     'Hello John! I''m happy to help. What would you like to know?', 
     CURRENT_TIMESTAMP - INTERVAL '3 hours' - INTERVAL '45 minutes'),
    ('ca000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 
     'What are my chances of getting primary custody?', 
     CURRENT_TIMESTAMP - INTERVAL '3 hours' - INTERVAL '30 minutes'),
    ('ca000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 
     'Based on the evidence we have gathered, your case is strong. Let''s discuss this in detail at our next meeting.', 
     CURRENT_TIMESTAMP - INTERVAL '3 hours'),
    
    -- Chat 2: Maria Silva and Pedro Advocate
    ('ca000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 
     'I received the contract analysis. Thank you!', 
     CURRENT_TIMESTAMP - INTERVAL '2 days'),
    ('ca000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 
     'You''re welcome! Do you have any questions about the findings?', 
     CURRENT_TIMESTAMP - INTERVAL '2 days' + INTERVAL '30 minutes'),
    ('ca000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 
     'Yes, can we schedule a call to discuss the next steps?', 
     CURRENT_TIMESTAMP - INTERVAL '1 day'),
    
    -- Chat 3: Carlos Santos and Rita Counsel
    ('ca000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', 
     'Hello Carlos, I''ve reviewed your termination documents. We have a strong case.', 
     CURRENT_TIMESTAMP - INTERVAL '10 days'),
    ('ca000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 
     'That''s great to hear! What are the next steps?', 
     CURRENT_TIMESTAMP - INTERVAL '10 days' + INTERVAL '1 hour'),
    ('ca000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003', 'c0000000-0000-0000-0000-000000000003', 
     'We''ll need to file a formal complaint. I''ll prepare the documents this week.', 
     CURRENT_TIMESTAMP - INTERVAL '9 days'),
    
    -- Chat 4: Maria Silva and Ana Lawyer (estate planning)
    ('ca000000-0000-0000-0000-000000000004', 'c0000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 
     'I saw the will draft you uploaded. It looks comprehensive!', 
     CURRENT_TIMESTAMP - INTERVAL '3 days'),
    ('ca000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 
     'Thank you! Please review it carefully and let me know if you want any changes.', 
     CURRENT_TIMESTAMP - INTERVAL '3 days' + INTERVAL '20 minutes'),
    
    -- Chat 5: John Doe and Pedro Advocate
    ('ca000000-0000-0000-0000-000000000005', 'c0000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 
     'Hi, I need some advice on a property contract.', 
     CURRENT_TIMESTAMP - INTERVAL '120 days'),
    ('ca000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 
     'Of course! Please send me the contract and I''ll review it.', 
     CURRENT_TIMESTAMP - INTERVAL '120 days' + INTERVAL '30 minutes');

-- Summary
DO $$
BEGIN
    RAISE NOTICE '====================================';
    RAISE NOTICE 'Database seeded successfully!';
    RAISE NOTICE '====================================';
    RAISE NOTICE 'Users created: 7 (1 admin, 3 clients, 3 lawyers)';
    RAISE NOTICE 'Processes: 5 (4 active, 1 closed)';
    RAISE NOTICE 'Documents: 9';
    RAISE NOTICE 'Appointments: 5';
    RAISE NOTICE 'Notifications: 6 (3 read, 3 unread)';
    RAISE NOTICE 'Chats: 5';
    RAISE NOTICE 'Messages: 14';
    RAISE NOTICE '====================================';
    RAISE NOTICE 'Default password for all users: password123';
    RAISE NOTICE '====================================';
END $$;

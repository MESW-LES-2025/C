-- PostgreSQL Database Schema for Legal Practice Management System

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Drop tables in reverse order of dependencies (for clean recreates)
DROP TABLE IF EXISTS Messages CASCADE;
DROP TABLE IF EXISTS Chats CASCADE;
DROP TABLE IF EXISTS Notifications CASCADE;
DROP TABLE IF EXISTS Appointments CASCADE;
DROP TABLE IF EXISTS Documents CASCADE;
DROP TABLE IF EXISTS Processes CASCADE;
DROP TABLE IF EXISTS Admins CASCADE;
DROP TABLE IF EXISTS Lawyers CASCADE;
DROP TABLE IF EXISTS Clients CASCADE;
DROP TABLE IF EXISTS Users CASCADE;

-- Create Users table (parent table for inheritance)
CREATE TABLE Users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    user_type VARCHAR(20) NOT NULL CHECK (user_type IN ('client', 'lawyer', 'admin')),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

-- Create Clients table
CREATE TABLE Clients (
    user_id UUID PRIMARY KEY,
    address TEXT,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE
);

-- Create Lawyers table
CREATE TABLE Lawyers (
    user_id UUID PRIMARY KEY,
    nif VARCHAR(9) NOT NULL,
    description TEXT,
    photo TEXT,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE,
    CONSTRAINT nif_format CHECK (nif ~ '^[0-9]{9}$'),
    CONSTRAINT uniq_lawyer_nif UNIQUE (nif)
);

-- Create Admins table
CREATE TABLE Admins (
    user_id UUID PRIMARY KEY,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE
);

-- Create Processes table
CREATE TABLE Processes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    client_id UUID NOT NULL,
    lawyer_id UUID NOT NULL,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    closed_at TIMESTAMP,
    steps_count INTEGER NOT NULL DEFAULT 0,
    current_step INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (client_id) REFERENCES Clients(user_id) ON DELETE CASCADE,
    FOREIGN KEY (lawyer_id) REFERENCES Lawyers(user_id) ON DELETE CASCADE,
    CONSTRAINT valid_steps CHECK (current_step >= 0 AND current_step <= steps_count),
    CONSTRAINT positive_steps_count CHECK (steps_count >= 0),
    CONSTRAINT valid_close_date CHECK (closed_at IS NULL OR closed_at >= created_at)
);

-- Create Documents table
CREATE TABLE Documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    process_id UUID NOT NULL,
    file_path TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (process_id) REFERENCES Processes(id) ON DELETE CASCADE,
    CONSTRAINT valid_file_path CHECK (LENGTH(TRIM(file_path)) > 0)
);

-- Create Appointments table
CREATE TABLE Appointments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    client_id UUID NOT NULL,
    lawyer_id UUID NOT NULL,
    process_id UUID,
    scheduled_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES Clients(user_id) ON DELETE CASCADE,
    FOREIGN KEY (lawyer_id) REFERENCES Lawyers(user_id) ON DELETE CASCADE,
    FOREIGN KEY (process_id) REFERENCES Processes(id) ON DELETE SET NULL,
    CONSTRAINT future_appointment CHECK (scheduled_at > created_at)
);

-- Create Notifications table
CREATE TABLE Notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    process_id UUID NOT NULL,
    client_id UUID NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (process_id) REFERENCES Processes(id) ON DELETE CASCADE,
    FOREIGN KEY (client_id) REFERENCES Clients(user_id) ON DELETE CASCADE,
    CONSTRAINT valid_content CHECK (LENGTH(TRIM(content)) > 0)
);

-- Create Chats table
CREATE TABLE Chats (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    client_id UUID NOT NULL,
    lawyer_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (client_id) REFERENCES Clients(user_id) ON DELETE CASCADE,
    FOREIGN KEY (lawyer_id) REFERENCES Lawyers(user_id) ON DELETE CASCADE,
    UNIQUE(client_id, lawyer_id)
);

-- Create Messages table
CREATE TABLE Messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    chat_id UUID NOT NULL,
    sender_id UUID NOT NULL,
    receiver_id UUID NOT NULL,
    content TEXT NOT NULL,
    sent_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (chat_id) REFERENCES Chats(id) ON DELETE CASCADE,
    FOREIGN KEY (sender_id) REFERENCES Users(id) ON DELETE CASCADE,
    FOREIGN KEY (receiver_id) REFERENCES Users(id) ON DELETE CASCADE,
    CONSTRAINT different_users CHECK (sender_id != receiver_id),
    CONSTRAINT valid_message_content CHECK (LENGTH(TRIM(content)) > 0)
);

-- Create indexes for better query performance
CREATE INDEX idx_users_email ON Users(email);
CREATE INDEX idx_users_type ON Users(user_type);
CREATE INDEX idx_processes_client ON Processes(client_id);
CREATE INDEX idx_processes_lawyer ON Processes(lawyer_id);
CREATE INDEX idx_processes_dates ON Processes(created_at, closed_at);
CREATE INDEX idx_documents_process ON Documents(process_id);
CREATE INDEX idx_appointments_client ON Appointments(client_id);
CREATE INDEX idx_appointments_lawyer ON Appointments(lawyer_id);
CREATE INDEX idx_appointments_scheduled ON Appointments(scheduled_at);
CREATE INDEX idx_notifications_client ON Notifications(client_id);
CREATE INDEX idx_notifications_process ON Notifications(process_id);
CREATE INDEX idx_chats_client ON Chats(client_id);
CREATE INDEX idx_chats_lawyer ON Chats(lawyer_id);
CREATE INDEX idx_messages_chat ON Messages(chat_id);
CREATE INDEX idx_messages_sent ON Messages(sent_at);

-- Add comments for documentation
COMMENT ON TABLE Users IS 'Base table for all system users';
COMMENT ON TABLE Clients IS 'Client-specific user information';
COMMENT ON TABLE Lawyers IS 'Lawyer-specific user information';
COMMENT ON COLUMN Lawyers.nif IS 'Portuguese NIF (Número de Identificação Fiscal), 9 numeric digits, unique';
COMMENT ON TABLE Admins IS 'Admin-specific user information';
COMMENT ON TABLE Processes IS 'Legal processes/cases managed in the system';
COMMENT ON TABLE Documents IS 'Documents associated with processes';
COMMENT ON TABLE Appointments IS 'Scheduled appointments between clients and lawyers';
COMMENT ON TABLE Notifications IS 'Notifications sent to clients about their processes';
COMMENT ON TABLE Chats IS 'Chat sessions between clients and lawyers';
COMMENT ON TABLE Messages IS 'Individual messages within chats';

-- Triggers

-- Trigger to validate chat participants match sender/receiver
CREATE OR REPLACE FUNCTION validate_message_participants()
RETURNS TRIGGER AS $$
DECLARE
    chat_client_id UUID;
    chat_lawyer_id UUID;
BEGIN
    -- Get the client and lawyer from the chat
    SELECT client_id, lawyer_id INTO chat_client_id, chat_lawyer_id
    FROM Chats
    WHERE id = NEW.chat_id;
    
    -- Ensure sender and receiver are participants in this chat
    IF (NEW.sender_id != chat_client_id AND NEW.sender_id != chat_lawyer_id) OR
       (NEW.receiver_id != chat_client_id AND NEW.receiver_id != chat_lawyer_id) THEN
        RAISE EXCEPTION 'Sender and receiver must be participants in the chat';
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_message_participants
    BEFORE INSERT ON Messages
    FOR EACH ROW
    EXECUTE FUNCTION validate_message_participants();

-- Trigger to prevent appointments in the past
CREATE OR REPLACE FUNCTION prevent_past_appointments()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.scheduled_at < CURRENT_TIMESTAMP THEN
        RAISE EXCEPTION 'Cannot schedule appointments in the past';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_appointment_time
    BEFORE INSERT ON Appointments
    FOR EACH ROW
    EXECUTE FUNCTION prevent_past_appointments();

-- Trigger to prevent updating closed_at to earlier than created_at
CREATE OR REPLACE FUNCTION validate_process_dates()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.closed_at IS NOT NULL AND NEW.closed_at < NEW.created_at THEN
        RAISE EXCEPTION 'Process cannot be closed before it was created';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_process_dates
    BEFORE INSERT OR UPDATE ON Processes
    FOR EACH ROW
    EXECUTE FUNCTION validate_process_dates();

-- Trigger to ensure only clients or lawyers can send messages
CREATE OR REPLACE FUNCTION validate_message_sender_type()
RETURNS TRIGGER AS $$
DECLARE
    sender_type VARCHAR(20);
BEGIN
    SELECT user_type INTO sender_type
    FROM Users
    WHERE id = NEW.sender_id;
    
    IF sender_type NOT IN ('client', 'lawyer') THEN
        RAISE EXCEPTION 'Only clients and lawyers can send messages';
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_message_sender_type
    BEFORE INSERT ON Messages
    FOR EACH ROW
    EXECUTE FUNCTION validate_message_sender_type();
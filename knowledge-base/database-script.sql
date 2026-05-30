CREATE DATABASE rag_chatbot_db;

-- Connect to database
-- \c rag_chatbot_db;

/* =========================================================
   EXTENSIONS
========================================================= */

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

/* =========================================================
   USERS & AUTHENTICATION
========================================================= */

CREATE TABLE roles
(
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

CREATE TABLE users
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    full_name VARCHAR(150) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,

    is_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NULL
);

CREATE TABLE user_roles
(
    user_id UUID NOT NULL,
    role_id INT NOT NULL,

    PRIMARY KEY (user_id, role_id),

    CONSTRAINT fk_user_roles_users
        FOREIGN KEY (user_id)
            REFERENCES users(id)
            ON DELETE CASCADE,

    CONSTRAINT fk_user_roles_roles
        FOREIGN KEY (role_id)
            REFERENCES roles(id)
            ON DELETE CASCADE
);

/* =========================================================
   SUBSCRIPTION & PAYMENT
========================================================= */

CREATE TABLE subscription_plans
(
    id SERIAL PRIMARY KEY,

    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),

    price NUMERIC(18,2) NOT NULL,

    daily_question_limit INT NOT NULL,

    allow_benchmark BOOLEAN NOT NULL DEFAULT FALSE,
    allow_advanced_models BOOLEAN NOT NULL DEFAULT FALSE,

    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE user_subscriptions
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id UUID NOT NULL,
    plan_id INT NOT NULL,

    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,

    status VARCHAR(50) NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_user_subscriptions_users
        FOREIGN KEY (user_id)
            REFERENCES users(id),

    CONSTRAINT fk_user_subscriptions_plans
        FOREIGN KEY (plan_id)
            REFERENCES subscription_plans(id),

    CONSTRAINT ck_user_subscriptions_status
        CHECK (status IN ('Pending', 'Active', 'Expired', 'Cancelled'))
);

CREATE TABLE payment_transactions
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    subscription_id UUID NOT NULL,

    amount NUMERIC(18,2) NOT NULL,

    payment_method VARCHAR(50) NOT NULL,
    payment_status VARCHAR(50) NOT NULL,

    transaction_code VARCHAR(255),

    paid_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_payment_transactions_subscriptions
        FOREIGN KEY (subscription_id)
            REFERENCES user_subscriptions(id),

    CONSTRAINT ck_payment_transactions_status
        CHECK (payment_status IN ('Pending', 'Paid', 'Failed', 'Refunded'))
);

/* =========================================================
   SUBJECTS & DOCUMENTS
========================================================= */

CREATE TABLE subjects
(
    id SERIAL PRIMARY KEY,

    subject_code VARCHAR(50) NOT NULL UNIQUE,
    subject_name VARCHAR(255) NOT NULL,

    description TEXT,

    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE chapters
(
    id SERIAL PRIMARY KEY,

    subject_id INT NOT NULL,

    chapter_name VARCHAR(255) NOT NULL,
    chapter_number INT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_chapters_subjects
        FOREIGN KEY (subject_id)
            REFERENCES subjects(id)
            ON DELETE CASCADE
);

CREATE TABLE documents
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    subject_id INT NOT NULL,
    chapter_id INT NULL,

    uploaded_by UUID NOT NULL,

    file_name VARCHAR(255) NOT NULL,
    original_file_name VARCHAR(255) NOT NULL,

    file_type VARCHAR(50) NOT NULL,
    file_size BIGINT NULL,

    file_path TEXT NOT NULL,

    is_indexed BOOLEAN NOT NULL DEFAULT FALSE,

    uploaded_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_documents_subjects
        FOREIGN KEY (subject_id)
            REFERENCES subjects(id),

    CONSTRAINT fk_documents_chapters
        FOREIGN KEY (chapter_id)
            REFERENCES chapters(id),

    CONSTRAINT fk_documents_users
        FOREIGN KEY (uploaded_by)
            REFERENCES users(id)
);

/* =========================================================
   DOCUMENT CHUNKS & VECTOR METADATA
========================================================= */

CREATE TABLE document_chunks
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    document_id UUID NOT NULL,

    chunk_index INT NOT NULL,

    chunk_text TEXT NOT NULL,

    embedding_model VARCHAR(100) NOT NULL,
    chunk_strategy VARCHAR(100) NOT NULL,

    vector_id VARCHAR(255) NULL,

    token_count INT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_document_chunks_documents
        FOREIGN KEY (document_id)
            REFERENCES documents(id)
            ON DELETE CASCADE
);

/* =========================================================
   CHAT & CONVERSATION
========================================================= */

CREATE TABLE conversations
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id UUID NOT NULL,

    title VARCHAR(255) NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NULL,

    CONSTRAINT fk_conversations_users
        FOREIGN KEY (user_id)
            REFERENCES users(id)
            ON DELETE CASCADE
);

CREATE TABLE messages
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    conversation_id UUID NOT NULL,

    sender_role VARCHAR(50) NOT NULL,

    content TEXT NOT NULL,

    prompt_tokens INT NULL,
    completion_tokens INT NULL,

    sent_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_messages_conversations
        FOREIGN KEY (conversation_id)
            REFERENCES conversations(id)
            ON DELETE CASCADE,

    CONSTRAINT ck_messages_sender_role
        CHECK (sender_role IN ('User', 'Assistant', 'System'))
);

CREATE TABLE citations
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    message_id UUID NOT NULL,
    document_id UUID NOT NULL,
    chunk_id UUID NOT NULL,

    quoted_text TEXT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_citations_messages
        FOREIGN KEY (message_id)
            REFERENCES messages(id)
            ON DELETE CASCADE,

    CONSTRAINT fk_citations_documents
        FOREIGN KEY (document_id)
            REFERENCES documents(id),

    CONSTRAINT fk_citations_chunks
        FOREIGN KEY (chunk_id)
            REFERENCES document_chunks(id)
);

/* =========================================================
   TEST SET & RESEARCH MODULE
========================================================= */

CREATE TABLE test_questions
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    question TEXT NOT NULL,
    ground_truth TEXT NOT NULL,

    difficulty VARCHAR(50) NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE experiments
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    experiment_name VARCHAR(255) NOT NULL,

    embedding_model VARCHAR(100) NOT NULL,
    chunk_strategy VARCHAR(100) NOT NULL,
    retrieval_method VARCHAR(100) NOT NULL,
    llm_model VARCHAR(100) NOT NULL,

    average_ragas_score DOUBLE PRECISION NULL,

    notes TEXT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE experiment_results
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    experiment_id UUID NOT NULL,
    test_question_id UUID NOT NULL,

    generated_answer TEXT NULL,

    faithfulness DOUBLE PRECISION NULL,
    answer_relevancy DOUBLE PRECISION NULL,
    context_precision DOUBLE PRECISION NULL,
    context_recall DOUBLE PRECISION NULL,

    latency_ms DOUBLE PRECISION NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_experiment_results_experiments
        FOREIGN KEY (experiment_id)
            REFERENCES experiments(id)
            ON DELETE CASCADE,

    CONSTRAINT fk_experiment_results_test_questions
        FOREIGN KEY (test_question_id)
            REFERENCES test_questions(id)
            ON DELETE CASCADE
);

/* =========================================================
   INDEXES
========================================================= */

CREATE INDEX ix_users_email
    ON users(email);

CREATE INDEX ix_documents_subject_id
    ON documents(subject_id);

CREATE INDEX ix_document_chunks_document_id
    ON document_chunks(document_id);

CREATE INDEX ix_conversations_user_id
    ON conversations(user_id);

CREATE INDEX ix_messages_conversation_id
    ON messages(conversation_id);

CREATE INDEX ix_citations_message_id
    ON citations(message_id);

CREATE INDEX ix_experiment_results_experiment_id
    ON experiment_results(experiment_id);

/* =========================================================
   SEED DATA
========================================================= */

INSERT INTO roles(name, description)
VALUES
    ('Admin', 'System administrator'),
    ('Lecturer', 'Lecturer role'),
    ('Student', 'Student role');

INSERT INTO subscription_plans
(
    name,
    description,
    price,
    daily_question_limit,
    allow_benchmark,
    allow_advanced_models
)
VALUES
    (
        'Free',
        'Basic free plan',
        0,
        10,
        FALSE,
        FALSE
    ),
    (
        'Basic',
        'Standard subscription plan',
        99000,
        100,
        TRUE,
        FALSE
    ),
    (
        'Premium',
        'Advanced premium plan',
        299000,
        999999,
        TRUE,
        TRUE
    );



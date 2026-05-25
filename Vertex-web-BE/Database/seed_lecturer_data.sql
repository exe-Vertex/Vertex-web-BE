-- Seed data for Lecturer Dashboard (FPT University)
-- Target Database: vertex_db
-- Run this script directly in pgAdmin, DBeaver, or psql to populate the database.

-- 1. Insert Projects
INSERT INTO projects (id, org_id, name, description, deadline, created_at, updated_at)
VALUES
('c1000000-0000-0000-0000-000000000003', 'e1000000-0000-0000-0000-000000000001', 'Mobile E-Commerce App', 'Develop UI prototypes and research features for school e-commerce platform', '2026-06-15', NOW(), NOW()),
('c1000000-0000-0000-0000-000000000004', 'e1000000-0000-0000-0000-000000000001', 'IoT Smart Greenhouse', 'Design sensor grid layouts and dashboard mockup for urban gardening', '2026-05-20', NOW(), NOW()),
('c1000000-0000-0000-0000-000000000005', 'e1000000-0000-0000-0000-000000000001', 'AI Chatbot Integration', 'Build support desk automation prototype using Gemini API', '2026-05-28', NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 2. Insert Project Members
INSERT INTO project_members (project_id, user_id, role, joined_at)
VALUES
('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000003', 'Leader', NOW()),
('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000007', 'Member', NOW()),
('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000005', 'Member', NOW()),

('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000007', 'Leader', NOW()),
('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000002', 'Member', NOW()),
('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000001', 'Member', NOW()),

('c1000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000001', 'Leader', NOW()),
('c1000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000003', 'Member', NOW())
ON CONFLICT (project_id, user_id) DO NOTHING;

-- 3. Insert Project Tasks
INSERT INTO tasks (id, project_id, title, description, status, priority, assignee_id, start_date, end_date, position, created_at, updated_at)
VALUES
-- Mobile E-Commerce App (On track, deadline 2026-06-15)
('d1000000-0000-0000-0000-000000000012', 'c1000000-0000-0000-0000-000000000003', 'User research & interviews', 'Interview 10 potential users.', 'done', 'high', 'a1000000-0000-0000-0000-000000000003', '2026-05-10', '2026-05-15', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000013', 'c1000000-0000-0000-0000-000000000003', 'Figma wireframes draft', 'Create initial wireframe layout.', 'ready-for-review', 'high', 'a1000000-0000-0000-0000-000000000007', '2026-05-16', '2026-05-20', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000014', 'c1000000-0000-0000-0000-000000000003', 'Architecture diagram', 'Define data flows and entity models.', 'in-progress', 'medium', 'a1000000-0000-0000-0000-000000000003', '2026-05-21', '2026-05-25', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000015', 'c1000000-0000-0000-0000-000000000003', 'Backend API endpoints doc', 'Write API document for frontend integration.', 'todo', 'medium', 'a1000000-0000-0000-0000-000000000005', '2026-05-26', '2026-06-01', 0, NOW(), NOW()),

-- IoT Smart Greenhouse (Overdue, deadline 2026-05-20)
('d1000000-0000-0000-0000-000000000016', 'c1000000-0000-0000-0000-000000000004', 'Literature review on hydroponic sensors', 'Compile datasheet summary.', 'done', 'low', 'a1000000-0000-0000-0000-000000000007', '2026-05-01', '2026-05-05', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000017', 'c1000000-0000-0000-0000-000000000004', 'Hardware bill of materials selection', 'Select best sensors and processors.', 'done', 'high', 'a1000000-0000-0000-0000-000000000001', '2026-05-06', '2026-05-10', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000018', 'c1000000-0000-0000-0000-000000000004', 'Layout diagram export', 'Map grid sensor placements in CAD.', 'ready-for-review', 'high', 'a1000000-0000-0000-0000-000000000002', '2026-05-11', '2026-05-14', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000019', 'c1000000-0000-0000-0000-000000000004', 'Power consumption simulation', 'Run standard gardening cycle load test.', 'todo', 'high', 'a1000000-0000-0000-0000-000000000007', '2026-05-15', '2026-05-19', 0, NOW(), NOW()),

-- AI Chatbot Integration (At risk, deadline 2026-05-28, only 3 days left with low progress)
('d1000000-0000-0000-0000-000000000020', 'c1000000-0000-0000-0000-000000000005', 'Identify core intent list', 'Outline 20 FAQ questions and trigger intents.', 'done', 'medium', 'a1000000-0000-0000-0000-000000000001', '2026-05-15', '2026-05-18', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000021', 'c1000000-0000-0000-0000-000000000005', 'GEMINI SDK integration test', 'Run diagnostic prompts via SDK.', 'in-progress', 'high', 'a1000000-0000-0000-0000-000000000003', '2026-05-19', '2026-05-24', 0, NOW(), NOW()),
('d1000000-0000-0000-0000-000000000022', 'c1000000-0000-0000-0000-000000000005', 'UI chatbot window component', 'Code floating chat frame in React.', 'todo', 'high', 'a1000000-0000-0000-0000-000000000001', '2026-05-25', '2026-05-28', 0, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

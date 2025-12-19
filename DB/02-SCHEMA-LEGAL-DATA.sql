-- ==============================================================================
-- PASSO 2.0 - DATA / SEED (IDEMPOTENT INSERTS)
-- ==============================================================================

-- Seed process types
INSERT INTO LEGAL.PROCESS_TYPE (process_type_name, process_type_is_active)
VALUES
  ('Civil', TRUE),
  ('Criminal', TRUE),
  ('Labor', TRUE),
  ('Administrative', TRUE)
ON CONFLICT (process_type_name) DO NOTHING;

-- Seed process phases
INSERT INTO LEGAL.PROCESS_PHASE (process_phase_name, process_phase_description, process_phase_is_active)
VALUES
  ('Initiation', 'Initial case filing and intake', TRUE),
  ('Discovery', 'Evidence collection and disclosure', TRUE),
  ('Trial', 'Trial phase and hearings', TRUE),
  ('Appeal', 'Appeal and post-trial activities', TRUE)
ON CONFLICT (process_phase_name) DO NOTHING;

-- Seed process statuses
INSERT INTO LEGAL.PROCESS_STATUS (process_status_name, process_status_is_final, process_status_is_default, process_status_is_active)
VALUES
  ('Open', FALSE, TRUE, TRUE),
  ('Suspended', FALSE, FALSE, TRUE),
  ('Closed', TRUE, FALSE, TRUE)
ON CONFLICT (process_status_name) DO NOTHING;

-- Build a few process_type_phase relations for the seeded data. Use the name lookups
-- to get ids and safe-upsert them into the bridging table (unique on process_type_id, process_phase_id)
WITH
  t AS (SELECT process_type_id FROM LEGAL.process_type WHERE process_type_name = 'Civil'),
  p1 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Initiation'),
  p2 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Discovery'),
  p3 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Trial')
INSERT INTO LEGAL.process_type_phase (process_phase_id, process_type_id, process_type_phase_order, process_type_phase_is_optional, process_type_phase_is_active)
SELECT p.process_phase_id, t.process_type_id, ord, FALSE, TRUE
FROM (
  SELECT p1.process_phase_id, 1 AS ord FROM p1
  UNION ALL SELECT p2.process_phase_id, 2 AS ord FROM p2
  UNION ALL SELECT p3.process_phase_id, 3 AS ord FROM p3
) p
CROSS JOIN t
ON CONFLICT (process_type_id, process_phase_id) DO NOTHING;

-- Add a mapping for 'Criminal' with different order
WITH
  t2 AS (SELECT process_type_id FROM LEGAL.process_type WHERE process_type_name = 'Criminal'),
  p1 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Initiation'),
  p2 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Trial')
INSERT INTO LEGAL.process_type_phase (process_phase_id, process_type_id, process_type_phase_order, process_type_phase_is_optional, process_type_phase_is_active)
SELECT p.process_phase_id, t2.process_type_id, ord, FALSE, TRUE
FROM (
  SELECT p1.process_phase_id, 1 AS ord FROM p1
  UNION ALL SELECT p2.process_phase_id, 2 AS ord FROM p2
) p
CROSS JOIN t2
ON CONFLICT (process_type_id, process_phase_id) DO NOTHING;

WITH
  t AS (SELECT process_type_id FROM LEGAL.process_type WHERE process_type_name = 'Labor'),
  p1 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Initiation'),
  p2 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Discovery'),
  p3 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Trial')
INSERT INTO LEGAL.process_type_phase (process_phase_id, process_type_id, process_type_phase_order, process_type_phase_is_optional, process_type_phase_is_active)
SELECT p.process_phase_id, t.process_type_id, ord, FALSE, TRUE
FROM (
  SELECT p1.process_phase_id, 1 AS ord FROM p1
  UNION ALL SELECT p2.process_phase_id, 2 AS ord FROM p2
  UNION ALL SELECT p3.process_phase_id, 3 AS ord FROM p3
) p
CROSS JOIN t
ON CONFLICT (process_type_id, process_phase_id) DO NOTHING;

WITH
  t AS (SELECT process_type_id FROM LEGAL.process_type WHERE process_type_name = 'Administrative'),
  p1 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Initiation'),
  p2 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Discovery'),
  p3 AS (SELECT process_phase_id FROM LEGAL.process_phase WHERE process_phase_name = 'Trial')
INSERT INTO LEGAL.process_type_phase (process_phase_id, process_type_id, process_type_phase_order, process_type_phase_is_optional, process_type_phase_is_active)
SELECT p.process_phase_id, t.process_type_id, ord, FALSE, TRUE
FROM (
  SELECT p1.process_phase_id, 1 AS ord FROM p1
  UNION ALL SELECT p2.process_phase_id, 2 AS ord FROM p2
  UNION ALL SELECT p3.process_phase_id, 3 AS ord FROM p3
) p
CROSS JOIN t
ON CONFLICT (process_type_id, process_phase_id) DO NOTHING;

-- Set default row for process_status if missing -- ensures at least Open exists
UPDATE LEGAL.process_status SET process_status_is_default = TRUE
WHERE process_status_name = 'Open';
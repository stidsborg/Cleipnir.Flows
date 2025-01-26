ALTER TABLE effects ADD COLUMN position INT;
UPDATE effects SET position = FLOOR(RAND() * 1000) + 1;
ALTER TABLE effects DROP PRIMARY KEY;
ALTER TABLE effects ADD PRIMARY KEY (type, instance, position);
ALTER TABLE effects DROP COLUMN id_hash;
UPDATE schema SET version = 5;

ALTER TABLE effects ADD COLUMN position INT;
UPDATE effects SET position = floor(random() * 1000) + 1;
ALTER TABLE effects DROP CONSTRAINT tickering_flow_effects_pkey;
ALTER TABLE effects ADD PRIMARY KEY (type, instance, position);
ALTER TABLE effects DROP COLUMN id_hash;
UPDATE schema SET version = 5;
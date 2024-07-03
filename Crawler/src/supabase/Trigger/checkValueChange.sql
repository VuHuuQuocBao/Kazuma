CREATE OR REPLACE FUNCTION check_update() RETURNS TRIGGER AS $$
BEGIN
  IF NEW."Lock" != OLD."Lock" THEN
    RETURN NEW;
  ELSE
    RAISE EXCEPTION 'New data is the same as old data!';
  END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER my_trigger
BEFORE UPDATE ON "MangaInfoGeneric"
FOR EACH ROW
EXECUTE FUNCTION check_update();
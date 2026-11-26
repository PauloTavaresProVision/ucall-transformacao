-- PostgreSQL. Run after a verified backup; removes only the two identified records.
-- Stop if the duplicate exists without the kept reference in the same area.
BEGIN;
DO $$
BEGIN
 IF EXISTS (SELECT 1 FROM "DevelopmentRequests" WHERE "Reference"='PD-20261030-DA69FF3C75') AND NOT EXISTS
 (SELECT 1 FROM "DevelopmentRequests" WHERE "Reference"='PD-20261030-F0B0345FE9' AND "Payload"::jsonb->>'area'='DC') THEN
  RAISE EXCEPTION 'O pedido de férias a conservar não foi encontrado na DC. Não remover.';
 END IF;
 IF EXISTS (SELECT 1 FROM "DevelopmentRequests" WHERE ("Reference"='PD-20261020-3956FEC098' AND ("Payload"::jsonb->>'area') IS DISTINCT FROM 'DP') OR ("Reference"='PD-20261030-DA69FF3C75' AND ("Payload"::jsonb->>'area') IS DISTINCT FROM 'DC')) THEN
  RAISE EXCEPTION 'Área inesperada. Não remover.';
 END IF;
END $$;
DELETE FROM "DevelopmentRequests" WHERE "Reference" IN ('PD-20261020-3956FEC098','PD-20261030-DA69FF3C75') RETURNING "Reference";
COMMIT;



SELECT "Id","EquipmentIdentifier","Name","Description","IsAvailable" FROM "Equipment" ORDER BY "Name","Id";

SELECT b."Id", s."StudentNumber", s."FullName", e."EquipmentIdentifier", e."Name", b."BorrowedAt", b."ExpectedReturnAt"
FROM "Borrowings" b JOIN "Students" s ON s."Id"=b."StudentId" JOIN "Equipment" e ON e."Id"=b."EquipmentId"
WHERE b."ReturnedAt" IS NULL ORDER BY b."ExpectedReturnAt", b."Id";

SELECT b."Id", b."StudentId", b."EquipmentId", b."BorrowedAt", b."ExpectedReturnAt", b."ReturnedAt", b."Status", e."IsAvailable"
FROM "Borrowings" b JOIN "Equipment" e ON e."Id"=b."EquipmentId" ORDER BY b."Id";

SELECT (SELECT count(*) FROM "Students") students, (SELECT count(*) FROM "Equipment") equipment,
       (SELECT count(*) FROM "Borrowings") borrowings,
       (SELECT count(*) FROM "Borrowings" WHERE "ReturnedAt" IS NULL) active,
       (SELECT count(*) FROM "Equipment" WHERE "IsAvailable") available_items;

SELECT * FROM "Borrowings" WHERE "Status"='Returned' ORDER BY "Id";

SELECT b.*, e."EquipmentIdentifier", e."Name" FROM "Borrowings" b JOIN "Equipment" e ON e."Id"=b."EquipmentId"
WHERE b."StudentId" = (SELECT "Id" FROM "Students" WHERE "StudentNumber"='2021-0002') ORDER BY b."BorrowedAt" DESC, b."Id";

SELECT b.*, s."StudentNumber", s."FullName" FROM "Borrowings" b JOIN "Students" s ON s."Id"=b."StudentId"
WHERE b."EquipmentId" = (SELECT "Id" FROM "Equipment" WHERE "EquipmentIdentifier"='PRJ-01') ORDER BY b."BorrowedAt" DESC, b."Id";

SELECT b."Id", s."FullName", s."StudentNumber", e."Name", b."BorrowedAt", b."ExpectedReturnAt", b."Status"
FROM "Borrowings" b JOIN "Students" s ON s."Id"=b."StudentId" JOIN "Equipment" e ON e."Id"=b."EquipmentId"
WHERE (s."FullName" ILIKE '%cruz%' OR s."StudentNumber" ILIKE '%cruz%' OR e."Name" ILIKE '%cruz%')
  AND b."Status" IN ('Active','Returned')
ORDER BY b."BorrowedAt" DESC, b."Id";

SELECT "Id","StudentNumber","StudentNumberNormalized","FullName","Contact","IsAllowedToBorrow" FROM "Students" ORDER BY "Id";

SELECT s."FullName", s."IsAllowedToBorrow", count(b."Id") FILTER (WHERE b."ReturnedAt" IS NULL) AS active
FROM "Students" s LEFT JOIN "Borrowings" b ON b."StudentId"=s."Id" GROUP BY s."Id" ORDER BY s."Id";

SELECT "Id","EquipmentIdentifier","EquipmentIdentifierNormalized","Name","Description","IsAvailable" FROM "Equipment" ORDER BY "Id";

SELECT * FROM "__EFMigrationsHistory";
SELECT conname, contype, pg_get_constraintdef(oid) FROM pg_constraint
WHERE conrelid IN ('"Students"'::regclass,'"Equipment"'::regclass,'"Borrowings"'::regclass) ORDER BY conrelid::text, conname;
SELECT indexname, indexdef FROM pg_indexes WHERE tablename IN ('Students','Equipment','Borrowings') ORDER BY indexname;

INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","ReturnedAt","Status")
VALUES (999999, (SELECT min("Id") FROM "Equipment"), CURRENT_DATE, CURRENT_DATE+7, NULL, 'Active');

INSERT INTO "Students" ("StudentNumber","StudentNumberNormalized","FullName","IsAllowedToBorrow")
VALUES (' 2021-0001 ','2021-0001','Duplicate Ana', true);

INSERT INTO "Equipment" ("EquipmentIdentifier","EquipmentIdentifierNormalized","Name","IsAvailable")
VALUES ('lap-01','LAP-01','Duplicate laptop', true);

INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","ReturnedAt","Status")
VALUES ((SELECT "Id" FROM "Students" WHERE "StudentNumber"='2021-0004'),
        (SELECT "Id" FROM "Equipment" WHERE "EquipmentIdentifier"='CAM-01'), CURRENT_DATE, CURRENT_DATE+7, NULL, 'Active');

INSERT INTO "Borrowings" ("StudentId","EquipmentId","BorrowedAt","ExpectedReturnAt","ReturnedAt","Status")
VALUES ((SELECT "Id" FROM "Students" WHERE "StudentNumber"='2021-0004'),
        (SELECT "Id" FROM "Equipment" WHERE "EquipmentIdentifier"='TRI-01'), CURRENT_DATE, CURRENT_DATE, NULL, 'Active');

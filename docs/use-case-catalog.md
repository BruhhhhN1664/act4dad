# Use-case catalog — Lab 4 classification

Primary actor of every required case: **Custodian**.

| ID | Use case | Lab 4 classification | Screen | Implemented by |
|---|---|---|---|---|
| UC-01 | View Equipment Catalog | Required continuation | Catalog | `EquipmentService.SearchAsync(text, false)` |
| UC-02 | Find Available Equipment | Required continuation | Catalog (“available only”) | `EquipmentService.SearchAsync(text, true)` |
| UC-03 | Borrow Equipment | Required continuation | Issue equipment (“Borrow one item”) | `BorrowingService.BorrowAsync` |
| UC-04 | View Active Borrowings | Required continuation | Active / Return | `BorrowingService.GetActiveAsync` |
| UC-05 | Return Equipment | Required continuation | Active / Return | `BorrowingService.ReturnAsync` |
| UC-06 | Check Out Multiple Equipment Items | **Required new** | Issue equipment (“Check out ticked items”) | `BorrowingService.CheckoutMultipleAsync` |
| UC-07 | View Student Borrowing History | **Required new** | History (left) | `BorrowingService.GetStudentHistoryAsync` |
| UC-08 | View Equipment Borrowing History | **Required new** | History (right) | `BorrowingService.GetEquipmentHistoryAsync` |
| UC-09 | Search, Filter, and Sort Borrowings | **Required new** | Search borrowings | `BorrowingService.SearchAsync` |
| UC-10 | View Overdue Borrowings | Deferred to Laboratory Activity 5 | – | – |
| UC-11 | View Current Equipment Holder | Deferred to Laboratory Activity 5 | – | – |
| UC-12 | Register Student | **Required new** | Students | `StudentService.RegisterAsync` |
| UC-13 | Update Student Information | **Required new** | Students | `StudentService.UpdateAsync` |
| UC-14 | Change Student Borrowing Eligibility | **Required new** | Students | `StudentService.SetEligibilityAsync` |
| UC-15 | Register Equipment | **Required new** | Equipment | `EquipmentService.RegisterAsync` |
| UC-16 | Update Equipment Information | **Required new** | Equipment | `EquipmentService.UpdateAsync` |
| UC-17 – UC-27 | Retire / maintenance / restore / condition / renew / return multiple / due soon / active count / utilization / categories / void | Deferred to Laboratory Activity 5 | – | – |
| UC-28 – UC-33 | Reservations, approvals, audit, staff accounts, exports, integrations | Future scope | – | – |

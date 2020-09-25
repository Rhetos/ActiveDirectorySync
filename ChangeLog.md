# ActiveDirectorySync release notes

## 2.0.0 (2020-09-25)

* Bugfix: SqlException "Cannot insert duplicate key row in object 'Common.PrincipalHasRole' with unique index 'IX_PrincipalHasRole_Principal_Role'." on multiple web requests.
The error is originated from RecomputeFromActiveDirectoryPrincipalHasRole. The error occurs when a new user or a new user role is added (mapped to Active Directory), Rhetos authorization cache is empty or invalidated, and multiple threads concurrently require authorization for the same user, resulting with parallel recomputes that read no matching record from PrincipalHasRole and try to insert a new one.

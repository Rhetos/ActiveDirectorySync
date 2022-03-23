# ActiveDirectorySync

ActiveDirectorySync is a plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It synchronizes the Rhetos principals and roles with Active Directory by automatically adding or removing principal-role and role-role membership relations.

## Installation

Installing this package to a Rhetos application:

1. Add 'Rhetos.ActiveDirectorySync' NuGet package, available at the [NuGet.org](https://www.nuget.org/) on-line gallery.

## Configuring Rhetos users and user groups

1. To allow a **domain user** to use Rhetos application, insert the record in the `Common.Principal` entity.
   The principal's name must have domain name prefix.
2. To allow a **domain user group** to be used for assigning permissions to users, insert the record in the `Common.Role` entity.
   The role's name must have domain name prefix.
3. ActiveDirectorySync will automatically handle relation between the inserted principals and role, based on information from Active Directory.

To set the users permissions, the following methods are available:

1. Set the users permissions directly, inserting the record in `Common.PrincipalPermission`.
2. Set the user's group permissions, inserting the record in `Common.RolePermission`.
3. Create a group of permissions and assign it to the user or user group:
    * Add a new `Common.Role` without domain name prefix (it will not be bound to the domain user group) that will serve as a permission group.
    * Set the role's permissions in `Common.RolePermission`.
    * Assign the role to the user's group (insert in `Common.RoleInheritsRole`) or directly to the user (insert in `Common.PrincipalHasRole`).

## How to contribute

Contributions are very welcome. The easiest way is to fork this repo, and then
make a pull request from your fork. The first time you make a pull request, you
may be asked to sign a Contributor Agreement.
For more info see [How to Contribute](https://github.com/Rhetos/Rhetos/wiki/How-to-Contribute) on Rhetos wiki.

### Building and testing the source code

* Note: This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
  You don't need to build it from source in order to use it in your application.
* To build the package from source, run `Clean.bat`, `Build.bat` and `Test.bat`.
* For the test script to work, you need to create an empty database and
  a settings file `test\Rhetos.ActiveDirectorySync.TestApp\rhetos-app.local.settings.json`
  with the database connection string (configuration key "ConnectionStrings:RhetosConnectionString").
* The build output is a NuGet package in the "Install" subfolder.

# ActiveDirectorySync

ActiveDirectorySync is a plugin package for [Rhetos development platform](https://github.com/Rhetos/Rhetos).
It synchronizes the Rhetos principals and roles with Active Directory by automatically adding or removing principal-role and role-role membership relations.

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

## Build

**Note:** This package is already available at the [NuGet.org](https://www.nuget.org/) online gallery.
You don't need to build it from source in order to use it in your application.

To build the package from source, run `Build.bat`.
The script will pause in case of an error.
The build output is a NuGet package in the "Install" subfolder.

## Installation

To install this package to a Rhetos server, add it to the Rhetos server's *RhetosPackages.config* file
and make sure the NuGet package location is listed in the *RhetosPackageSources.config* file.

* The package ID is "**Rhetos.ActiveDirectorySync**".
  This package is available at the [NuGet.org](https://www.nuget.org/) online gallery.
  It can be downloaded or installed directly from there.
* For more information, see [Installing plugin packages](https://github.com/Rhetos/Rhetos/wiki/Installing-plugin-packages).

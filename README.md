# Identity Management App #
This identity management app is built with ASP.NET Identity and SQL Server, featuring a custom user class, roles, and policies. It uses JWT authentication and includes email confirmation using an SMTP server. The app provides various API endpoints for user authentication and management.

## What I learned ##
* **ASP.NET Identity:** Gained experience implementing and customizing ASP.NET Identity for user management.
* **JWT Authentication:** Learned how to secure APIs using JSON Web Tokens.
* **Email Integration:** Configured and used SMTP servers for email confirmation.
* **Role-Based Access Control:** Implemented roles and policies to manage user permissions effectively.
* **API Development:** Developed RESTful API endpoints for user authentication and management.
* **Database Seeding:** Automated initial data seeding for predefined roles in the database.

## Features ##
* **Custom User Class:** Customizable user class for flexible user management.
* **Roles and Policies:** Implements roles such as Admin, Manager, Supervisor, and Member, with associated policies.
* **JWT Authentication:** Secure user authentication using JSON Web Tokens.
* **Email Confirmation:** Email confirmation functionality using an SMTP server.
* **Data Seeding:** Automatically seeds initial data for predefined roles.

## Installation ##
1. Clone the repository.
2. Update the connection string in `appsettings.json` to point to your SQL Server instance.
3. Configure the SMTP settings in `appsettings.json` for email confirmation.
4. Run the application.

## Usage ##
### Seeding Data ##
The application automatically seeds data for the following roles default users for each role:
* Admin
* Manager
* Supervisor
* Member

## API Endpoints ##
[API Documentation](https://github.com/gihan-aj/Identity-Management/blob/main/IdentityManagementApp/Documentation/ApiEndpoints.md)

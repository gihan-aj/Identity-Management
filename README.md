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
### Login ###
  * **Endpoint:** `/api/account/login`
  * **Method:** POST
  * **Request:**
  ```json
{
    "username": "admin@example.com",
    "password": "******"
}
  ```
  * **Response:**
    * 200 OK:
    ```json
    {
      "firstName": "admin",
      "lastName": "user",
      "jwt": "qehoeui"
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "Invalid username or password."
    }
    ```
    * 400 Bad Request: Validation Errors
    ```json
    {
      "error":
      {
          errors:
          [
            "Invalid username",
            "Username must be at least 3, and maximum 15 characters long."
          ]
      }
    }
    ```

### Register ###
  * **Endpoint:** `/api/account/register`
  * **Method:** POST
  * **Request:**
  ```json
{
    "firstName": "Admin",
    "lastName": "User",
    "email": "admin@example.com",
    "password": "*******"
}
  ```
  * **Response:**
    * 201 Created:
    ```json
    {
      "title": "Account Created",
      "message": "Your account has been created. please confirm your email address.",
    }
    ```
    * 400 Bad Request:
    ```json
    {
      "error": "Failed to send email. Please contact administration"
    }
    ```
    * 400 Bad Request: Validation Errors
    ```json
    {
      "error":
      {
          errors:
          [
            "Invalid email address",
            "Last name must be at least 3, and maximum 15 characters long."
          ]
      }
    }
    ```

### Confirm Email ###
  * **Endpoint:** `/api/account/confirm-email`
  * **Method:** PUT
  * **Request:**
  ```json
{
    "token": "fjhdjf",
    "email": "admin@example.com"
}
  ```
  * **Response:**
    * 200 OK:
    ```json
    {
      "title": "Email Confirmed",
      "message": "Your email address is confirmed. You can login now.",
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "This email has not been registered yet."
    }
    ```
    * 400 Bad Request:
    ```json
    {
      "error": "Your email was confirmed before. Please login to your account."
    }
    ```
    * 400 Bad Request: Validation Errors
    ```json
    {
      "error":
      {
          errors:
          [
            "Invalid email address"
          ]
      }
    }
    ```

### Resend Email Confirmation Link ###
  * **Endpoint:** `/api/account/resend-email-confirmation-link/{email}`
  * **Method:** POST
  * **Response:**
    * 200 OK:
    ```json
    {
      "title": "Confirmation link sent",
      "message": "Please confirm your email address.",
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "This email address has not been registered yet."
    }
    ```
    * 400 Bad Request:
    ```json
    {
      "error": "Invalid email address"
    }
    ```

### Forgot Username or Password ###
  * **Endpoint:** `/api/account/forgot-username-or-password/{email}`
  * **Method:** POST
  * **Response:**
    * 200 OK:
    ```json
    {
      "title": "Forgot username or password email sent",
      "message": "Please check your email",
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "This email address has not been registered yet."
    }
    ```
    * 400 Bad Request:
    ```json
    {
      "error": "Invalid email address"
    }
    ```

### Reset Password ###
  * **Endpoint:** `/api/account/reset-password`
  * **Method:** PUT
  * **Request:**
  ```json
{
    "token": "fjhdjf",
    "email": "admin@example.com",
    "newPassword":"******"
}
  ```
  * **Response:**
    * 200 OK:
    ```json
    {
      "title": "Password reset success",
      "message": "Your password has been reset.",
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "This email address has not been registered yet."
    }
    ```
    * 400 Bad Request:
    ```json
    {
      "error": "Please confirm your email first."
    }
    ```
    * 400 Bad Request: Validation Errors
    ```json
    {
      "error":
      {
          errors:
          [
            "Invalid email address"
          ]
      }
    }
    ```

### Refresh User Token ###
  * **Endpoint:** `/api/account/refresh-user-token`
  * **Headers:**
  ```
  Authorization: Bearer {jwt_token}

  ```
  * **Method:** GET
  * **Response:**
    * 200 OK:
    ```json
    {
      "firstName": "admin",
      "lastName": "user",
      "jwt": "qehoeui"
    }
    ```
    * 401 Unauthorized:
    ```json
    {
      "error": "Unauthorized"
    }
    ```



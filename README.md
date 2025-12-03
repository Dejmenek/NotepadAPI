# NotepadAPI

A minimal, secure RESTful API for managing personal notes.  
**This is an academic project** designed to demonstrate modern .NET API development, authentication, and CRUD operations.

---

## Table of Contents

- [General Information](#general-information)
- [Used Technologies](#used-technologies)
- [API Endpoints](#api-endpoints)
- [Authors](#authors)

---

## General Information

NotepadAPI is a simple note-taking backend built with ASP.NET Core.  
It allows users to register, authenticate using JWT, and perform CRUD operations on their personal notes.  
The project is intended for academic purposes, showcasing secure authentication, validation, and RESTful design using the latest .NET technologies.

---

## Used Technologies

- **.NET 9** (C# 13)
- **ASP.NET Core Minimal APIs**
- **Entity Framework Core** (with SQL Server)
- **ASP.NET Core Identity**
- **JWT (JSON Web Token) Authentication**
- **OpenAPI** (for API documentation)
- **Microsoft IdentityModel.Tokens**

---

 ## API Endpoints

### Authentication

#### POST `/login`
Authenticate a user and receive a JWT token.

**Request Body:**
``{ "email": "user@example.com", "password": "string" }``

**Responses:**
- `200 OK` – Returns JWT token as a string
- `400 Bad Request` – Validation or authentication failed

---

#### POST `/register`
Register a new user.

**Request Body:**
``{ "email": "user@example.com", "password": "string" }``

**Responses:**
- `200 OK` – Registration successful
- `400 Bad Request` – Validation or registration failed

---

### Notes (Require Authentication)

#### GET `/notes`
Get all notes for the authenticated user.

**Responses:**
- `200 OK` – Returns array of notes
- `401 Unauthorized` – If not authenticated

---

#### POST `/notes`
Create a new note.

**Request Body:**
``{ "content": "string" }``

**Responses:**
- `201 Created` – Returns created note
- `400 Bad Request` – Validation failed
- `401 Unauthorized` – If not authenticated

---

#### PUT `/notes/id`
Update an existing note by ID.

**Request Body:**
``{ "content": "string" }``

**Responses:**
- `204 No Content` – Update successful
- `400 Bad Request` – Validation failed
- `401 Unauthorized` – If not authenticated
- `403 Forbidden` – If not the note owner
- `404 Not Found` – If note does not exist

---

#### DELETE `notes/id`
Delete a note by ID.

**Responses:**
- `204 No Content` – Deletion successful
- `401 Unauthorized` – If not authenticated
- `403 Forbidden` – If not the note owner
- `404 Not Found` – If note does not exist
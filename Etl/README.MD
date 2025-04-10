# ETL Editor API Documentation

## Host

You can access the API at the following address:

*Base URL:*  
[http://localhost:5000/](http://localhost:5000/)

---

## Controllers

### 1. Retrieve XML Forest Structure from \etl_editor\

*Route:*  
\GET api/data\

*Description:*  
This endpoint retrieves the XML forest structure from the \etl_editor\ database.

*Request Body:*  
The following JSON object must be sent as the request body:
````
json
{
"TaskId": <int>,                    // Task ID in ETL (e.g., 216 for КПТ)
"Host": "<string>",                 // Database host
"Port": <int>,                     // Database port number
"Username": "<string>",             // Database username
"Password": "<string>",             // Database password
"Database": "<string>",             // Database name
"SshHost": "<string>",              // SSH host
"SshPort": <int>,                  // SSH port number
"SshUsername": "<string>",          // SSH username
"SshPassword": "<string>"           // SSH password
}
````
### Example Request

````
http
POST http://localhost:5000/api/data
Content-Type: application/json

{
    "TaskId": 216,
    "Host": "your_database_host",
    "Port": 5432,
    "Username": "your_username",
    "Password": "your_password",
    "Database": "your_database",
    "SshHost": "your_ssh_host",
    "SshPort": 22,
    "SshUsername": "your_ssh_username",
    "SshPassword": "your_ssh_password"
}
````
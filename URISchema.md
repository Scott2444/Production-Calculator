# Routes
The following are the route structure for the frontend and backend api resources.

pub_ids are 10 char strings in base58 (4e17 possible numbers)

# Frontend
## Users
/{username}/{project_name}/workflows/{pub_workflow_id}

# Backend
## Auth
/auth/login
/auth/refresh
## Main Resources
/users/{pub_user_id}
/projects/{pub_project_id}
/workflows/{pub_workflow_id}

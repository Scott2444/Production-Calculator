# Routes
The following are the route structure for the frontend and backend api resources.

pub_ids are 10 char strings in base58 (4e17 possible numbers)

# Frontend
## Users
/{username}/{project_name}/workflows/{pub_workflow_id}
/{username}/{project_name}/recipes/{pub_recipe_id}
/{username}/{project_name}/machines/{pub_machines_id}
/{username}/{project_name}/modifiers/{pub_modifiers_id}
/{username}/{project_name}/products/{pub_products_id}

# Backend
## Auth
/auth/login
/auth/refresh
## Main Resources
/users/{pub_user_id}
/projects/{pub_project_id}
/projects/{pub_project_id}/workflows/{pub_workflow_id}
/projects/{pub_project_id}/recipes/{pub_recipe_id}
/projects/{pub_project_id}/machines/{pub_machine_id}
/projects/{pub_project_id}/modifiers/{pub_modifier_id}
/projects/{pub_project_id}/products/{pub_product_id}
# Routes

The following are the route structure for the frontend and backend api resources.

pub_ids are 10 char strings in base58 (4e17 possible numbers)

# Frontend

## General

/login
/register
/home
/explore
/settings

## Users

/{username}/{project_name}/workflows/{workflow_name}
/{username}/{project_name}/recipes/
/{username}/{project_name}/machines/
/{username}/{project_name}/modifiers/
/{username}/{project_name}/products/

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

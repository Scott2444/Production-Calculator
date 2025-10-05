# Production-Calculator

## Summary
This full-stack project will enable users to create projects of production pipelines for management style games similar to Factorio, Satisfactory, or Dyson Sphere Program. It will provide a platform to input recipes, create output requirements, and calculate production line designs. These projects will be publically accessible by other users.

## Architecture

### Frontend
This uses Next.js framework with client-side rendering (main calculator interface) and server-side rendering (shareable proejct pages).
The styling is done in Tailwind CSS.
The API communication uses Axios and JWT for authentication.
The state management is done with React Context API + Hooks.
For data visualization, this uses React Flow or D3.js???

### Backend
The framework will use .NET Core Web API and EF Core for database communication.
Authentication will be done with JWT.

### Database
This will use PostgreSQL.
#### Entity Relation Diagram
https://michiganstate-my.sharepoint.com/personal/haakens3_msu_edu/_layouts/15/Doc.aspx?sourcedoc={a9564c30-e26d-4955-9261-0c8381a41fcf}&action=embedview

## Hosting
These are just recommendations, likely to change. 

### Frontend
Deployed on AWS Amplify

### Backend
Stored in Dockerfile on AWS ECR
Deployed on AWS Fargate

### Database
Deployed on AWS RDS
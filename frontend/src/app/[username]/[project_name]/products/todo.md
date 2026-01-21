### Shared Components

1. Use ProjectPageLayout.tx for a project context hook.

- use it for url parameter extraction, username, project_name
- project queries
- current project resolution

2. Create useSearch() hook that takes items array and returns { searchText, setSearchText, filteredItems }
3. Crud State Management

- Create useCrudState() hook for managing opertions

4. Delete confirmation logic

- useDeleteConfirmation() hook

5. Loading/Error States

- Project loading/error messages
- Item loading/error messages
- Empty states

### Shared UI

1. Search Bar
2. (filtered search) Item Grid/Card with edit/delete buttons
3. Common cancel/create/edit button patterns
4. Error display with dismiss button

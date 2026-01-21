### Shared Components

(DONE)

1. Use ProjectPageLayout.tx for a project context hook.

- use it for url parameter extraction, username, project_name
- project queries
- current project resolution

(Created, not implemented)

2. Project Context Hook

- URL parameter extraction (username, project_name)
- Projects query
- Current project resolution
- projectId derivation
- canEdit logic

3. Create useSearch() hook that takes items array and returns { searchText, setSearchText, filteredItems }
4. Crud State Management

- Create useCrudState() hook for managing opertions

5. Delete confirmation logic

- useDeleteConfirmation() hook

6. Loading/Error States

- Project loading/error messages
- Item loading/error messages
- Empty states

### Shared UI

1. Search Bar
2. (filtered search) Item Grid/Card with edit/delete buttons
3. Common cancel/create/edit button patterns
4. Error display with dismiss button

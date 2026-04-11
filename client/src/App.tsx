import { useState, useEffect, useCallback } from 'react';
import { getTasks, getUsers, getStatuses, createTask, updateTask, deleteTask, moveTask } from './services/taskService';
import type { TaskDto, UserDto, CreateTaskRequest } from './types';
import type { StatusDto } from './services/taskService';
import './App.css';

const STATUSES = ['To-do', 'Doing', 'Review', 'Done'];

const STATUS_COLORS: Record<string, string> = {
  'To-do':  '#6b7280',
  'Doing':  '#3b82f6',
  'Review': '#f59e0b',
  'Done':   '#10b981',
};

function App() {
  const [tasks,    setTasks]    = useState<TaskDto[]>([]);
  const [users,    setUsers]    = useState<UserDto[]>([]);
  const [statuses, setStatuses] = useState<StatusDto[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState<string | null>(null);

  const [showModal,    setShowModal]    = useState(false);
  const [editingTask,  setEditingTask]  = useState<TaskDto | null>(null);
  const [form,         setForm]         = useState<CreateTaskRequest>({ title: '', description: '', assigneeId: undefined });
  const [submitting,   setSubmitting]   = useState(false);

  const [movingTask,   setMovingTask]   = useState<TaskDto | null>(null);
  const [selectedUser, setSelectedUser] = useState<string>('');

  const [search,         setSearch]         = useState('');
  const [filterAssignee, setFilterAssignee] = useState('');

  const load = useCallback(async () => {
    try {
      setError(null);
      const [t, u, s] = await Promise.all([getTasks(), getUsers(), getStatuses()]);
      setTasks(t);
      setUsers(u);
      setStatuses(s);
    } catch (e: any) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openCreate = () => {
    setEditingTask(null);
    setForm({ title: '', description: '', assigneeId: undefined });
    setShowModal(true);
  };

  const openEdit = (task: TaskDto) => {
    setEditingTask(task);
    setForm({ title: task.title, description: task.description ?? '', assigneeId: task.assignee?.id });
    setShowModal(true);
  };

  const openMove = (task: TaskDto) => {
    setMovingTask(task);
    setSelectedUser(users[0]?.id ?? '');
  };

  const handleSubmit = async () => {
    if (!form.title.trim()) return;
    setSubmitting(true);
    try {
      if (editingTask) {
        await updateTask(editingTask.id, { title: form.title, description: form.description, assigneeId: form.assigneeId });
      } else {
        await createTask({ title: form.title, description: form.description, assigneeId: form.assigneeId });
      }
      setShowModal(false);
      await load();
    } catch (e: any) {
      alert('Error: ' + e.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleMove = async (newStatusId: string) => {
    if (!movingTask || !selectedUser) return;
    try {
      await moveTask(movingTask.id, newStatusId, selectedUser);
      setMovingTask(null);
      await load();
    } catch (e: any) {
      alert('Error: ' + e.message);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this task?')) return;
    try {
      await deleteTask(id);
      await load();
    } catch (e: any) {
      alert('Error: ' + e.message);
    }
  };

  const filtered = tasks.filter(t => {
    const matchSearch = t.title.toLowerCase().includes(search.toLowerCase()) ||
        (t.description ?? '').toLowerCase().includes(search.toLowerCase());
    const matchAssignee = filterAssignee ? t.assignee?.id === filterAssignee : true;
    return matchSearch && matchAssignee;
  });

  const byStatus = (status: string) => filtered.filter(t => t.status === status);
  const formatDate = (iso: string) =>
      new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });

  const otherStatuses = movingTask
      ? statuses.filter(s => s.name !== movingTask.status)
      : [];

  return (
      <div className="app">
        <header className="header">
          <div className="header-left">
            <div className="logo">
              <span className="logo-icon">◈</span>
              <span className="logo-text">TaskFlow</span>
            </div>
            <span className="header-subtitle">Project Board</span>
          </div>
          <div className="header-right">
            <div className="stat-chip">
              <span className="stat-num">{tasks.length}</span>
              <span className="stat-label">total</span>
            </div>
            <div className="stat-chip accent">
              <span className="stat-num">{byStatus('In Progress').length}</span>
              <span className="stat-label">active</span>
            </div>
            <button className="btn-primary" onClick={openCreate}>+ New Task</button>
          </div>
        </header>

        <div className="filters">
          <input
              className="search-input"
              placeholder="Search tasks..."
              value={search}
              onChange={e => setSearch(e.target.value)}
          />
          <select
              className="filter-select"
              value={filterAssignee}
              onChange={e => setFilterAssignee(e.target.value)}
          >
            <option value="">All assignees</option>
            {users.map(u => <option key={u.id} value={u.id}>{u.username}</option>)}
          </select>
          {(search || filterAssignee) && (
              <button className="btn-clear" onClick={() => { setSearch(''); setFilterAssignee(''); }}>
                Clear filters
              </button>
          )}
        </div>

        {loading ? (
            <div className="center-msg">
              <div className="spinner" />
              <p>Loading board...</p>
            </div>
        ) : error ? (
            <div className="center-msg error">
              <span className="error-icon">⚠</span>
              <p>{error}</p>
              <button className="btn-primary" onClick={load}>Retry</button>
            </div>
        ) : (
            <div className="board">
              {STATUSES.map(status => {
                const cols = byStatus(status);
                return (
                    <div className="column" key={status}>
                      <div className="column-header">
                        <div className="column-title-row">
                          <span className="status-dot" style={{ background: STATUS_COLORS[status] }} />
                          <span className="column-title">{status}</span>
                          <span className="column-count">{cols.length}</span>
                        </div>
                      </div>
                      <div className="cards">
                        {cols.length === 0 ? (
                            <div className="empty-col">No tasks</div>
                        ) : (
                            cols.map(task => (
                                <div className="card" key={task.id}>
                                  <div className="card-header">
                          <span
                              className="card-status-badge"
                              style={{ background: STATUS_COLORS[task.status] + '22', color: STATUS_COLORS[task.status] }}
                          >
                            {task.status}
                          </span>
                                    <div className="card-actions">
                                      <button className="icon-btn move" onClick={() => openMove(task)} title="Move">⇄</button>
                                      <button className="icon-btn edit" onClick={() => openEdit(task)} title="Edit">✎</button>
                                      <button className="icon-btn del"  onClick={() => handleDelete(task.id)} title="Delete">✕</button>
                                    </div>
                                  </div>
                                  <h3 className="card-title">{task.title}</h3>
                                  {task.description && <p className="card-desc">{task.description}</p>}
                                  <div className="card-footer">
                                    {task.assignee ? (
                                        <div className="assignee">
                                          <div className="avatar">{task.assignee.username[0].toUpperCase()}</div>
                                          <span className="assignee-name">{task.assignee.username}</span>
                                        </div>
                                    ) : (
                                        <span className="unassigned">Unassigned</span>
                                    )}
                                    <span className="card-date">{formatDate(task.createdAt)}</span>
                                  </div>
                                </div>
                            ))
                        )}
                      </div>
                    </div>
                );
              })}
            </div>
        )}

        {/* Create/Edit Modal */}
        {showModal && (
            <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setShowModal(false); }}>
              <div className="modal">
                <div className="modal-header">
                  <h2>{editingTask ? 'Edit Task' : 'New Task'}</h2>
                  <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
                </div>
                <div className="modal-body">
                  <label className="field-label">Title *</label>
                  <input
                      className="field-input"
                      placeholder="Task title"
                      value={form.title}
                      onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                      autoFocus
                  />
                  <label className="field-label">Description</label>
                  <textarea
                      className="field-input field-textarea"
                      placeholder="Optional description..."
                      value={form.description}
                      onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                      rows={3}
                  />
                  <label className="field-label">Assignee</label>
                  <select
                      className="field-input"
                      value={form.assigneeId ?? ''}
                      onChange={e => setForm(f => ({ ...f, assigneeId: e.target.value || undefined }))}
                  >
                    <option value="">Unassigned</option>
                    {users.map(u => <option key={u.id} value={u.id}>{u.username}</option>)}
                  </select>
                </div>
                <div className="modal-footer">
                  <button className="btn-ghost" onClick={() => setShowModal(false)}>Cancel</button>
                  <button
                      className="btn-primary"
                      onClick={handleSubmit}
                      disabled={submitting || !form.title.trim()}
                  >
                    {submitting ? 'Saving...' : editingTask ? 'Save Changes' : 'Create Task'}
                  </button>
                </div>
              </div>
            </div>
        )}

        {/* Move Modal */}
        {movingTask && (
            <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setMovingTask(null); }}>
              <div className="modal">
                <div className="modal-header">
                  <h2>Move Task</h2>
                  <button className="modal-close" onClick={() => setMovingTask(null)}>✕</button>
                </div>
                <div className="modal-body">
                  <p className="move-task-title">"{movingTask.title}"</p>
                  <p className="move-current">
                    Currently in <span style={{ color: STATUS_COLORS[movingTask.status] }}>{movingTask.status}</span>
                  </p>
                  <label className="field-label" style={{ marginTop: '16px' }}>Moved by</label>
                  <select
                      className="field-input"
                      value={selectedUser}
                      onChange={e => setSelectedUser(e.target.value)}
                  >
                    {users.map(u => <option key={u.id} value={u.id}>{u.username}</option>)}
                  </select>
                  <label className="field-label" style={{ marginTop: '16px' }}>Move to</label>
                  <div className="status-grid">
                    {otherStatuses.map(s => (
                        <button
                            key={s.id}
                            className="status-btn"
                            style={{ borderColor: STATUS_COLORS[s.name] ?? '#6b7280', color: STATUS_COLORS[s.name] ?? '#6b7280' }}
                            onClick={() => handleMove(s.id)}
                        >
                          <span className="status-btn-dot" style={{ background: STATUS_COLORS[s.name] ?? '#6b7280' }} />
                          {s.name}
                        </button>
                    ))}
                  </div>
                </div>
                <div className="modal-footer">
                  <button className="btn-ghost" onClick={() => setMovingTask(null)}>Cancel</button>
                </div>
              </div>
            </div>
        )}
      </div>
  );
}

export default App;

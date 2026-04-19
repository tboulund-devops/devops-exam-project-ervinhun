import { useEffect, useState } from 'react';
import type { TaskDto, TaskCommentDto, UserDto } from '../types';
import { getCommentsByTaskId, createComment } from '../services/taskService';

type Props = {
    task: TaskDto;
    users: UserDto[];
    onClose: () => void;
};

export default function TaskCommentsModal({ task, users, onClose }: Props) {
    const [comments, setComments] = useState<TaskCommentDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [text, setText] = useState('');
    const [userId, setUserId] = useState(users[0]?.id ?? '');
    const [submitting, setSubmitting] = useState(false);

    const loadComments = async () => {
        try {
            setLoading(true);
            const data = await getCommentsByTaskId(task.id);
            setComments(data);
        } catch (e: any) {
            alert('Error loading comments: ' + e.message);
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!text.trim()) return;

        setSubmitting(true);
        try {
            const created = await createComment(task.id, {
                content: text.trim(),
                userId: userId || undefined,
            });

            setComments(prev => [...prev, created]);
            setText('');
        } catch (e: any) {
            alert('Error creating comment: ' + e.message);
        } finally {
            setSubmitting(false);
        }
    };

    useEffect(() => {
        loadComments();
    }, [task.id]);

    const formatDateTime = (iso: string) =>
        new Date(iso).toLocaleString('en-GB', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });

    return (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
            <div className="modal">
                <div className="modal-header">
                    <h2>Comments</h2>
                    <button className="modal-close" onClick={onClose}>✕</button>
                </div>

                <div className="modal-body">
                    <p><strong>{task.title}</strong></p>

                    {loading ? (
                        <p>Loading...</p>
                    ) : comments.length === 0 ? (
                        <p>No comments yet.</p>
                    ) : (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
                            {comments.map(c => (
                                <div key={c.id} className="comment-box">
                                    <div><strong>{c.username ?? 'Unknown'}</strong></div>
                                    <div>{c.content}</div>
                                    <div className="comment-date">{formatDateTime(c.createdAt)}</div>
                                </div>
                            ))}
                        </div>
                    )}

                    <label className="field-label">User</label>
                    <select
                        className="field-input"
                        value={userId}
                        onChange={e => setUserId(e.target.value)}
                    >
                        <option value="">No user</option>
                        {users.map(u => (
                            <option key={u.id} value={u.id}>{u.username}</option>
                        ))}
                    </select>

                    <label className="field-label" style={{ marginTop: '12px' }}>New Comment</label>
                    <textarea
                        className="field-input field-textarea"
                        value={text}
                        onChange={e => setText(e.target.value)}
                        rows={4}
                    />
                </div>

                <div className="modal-footer">
                    <button className="btn-ghost" onClick={onClose}>Close</button>
                    <button
                        className="btn-primary"
                        onClick={handleCreate}
                        disabled={submitting || !text.trim()}
                    >
                        {submitting ? 'Posting...' : 'Add Comment'}
                    </button>
                </div>
            </div>
        </div>
    );
}
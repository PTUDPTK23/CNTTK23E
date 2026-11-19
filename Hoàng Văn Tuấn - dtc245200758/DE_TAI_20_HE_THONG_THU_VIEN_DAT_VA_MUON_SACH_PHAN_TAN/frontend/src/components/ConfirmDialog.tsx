import { Modal } from './Modal';

interface Props {
  open: boolean;
  title: string;
  message: string;
  confirmText?: string;
  danger?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({ open, title, message, confirmText = 'Xác nhận', danger = false, onConfirm, onCancel }: Props) {
  if (!open) return null;
  return (
    <Modal title={title} onClose={onCancel}>
      <p>{message}</p>
      <div className="row end">
        <button className="btn btn-outline" onClick={onCancel}>Đóng</button>
        <button className={danger ? 'btn btn-danger-solid' : 'btn'} onClick={onConfirm}>{confirmText}</button>
      </div>
    </Modal>
  );
}

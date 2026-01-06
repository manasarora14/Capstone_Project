import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from './auth-service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private snackBar = inject(MatSnackBar);
  private hubConnection!: signalR.HubConnection;
  private auth = inject(AuthService);
  
  
  notifications = signal<any[]>([]);
  unreadCount = signal<number>(0);

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7115/notificationHub', {
        accessTokenFactory: () => this.auth.getToken() || '',
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (message: string) => {
      if (message) this.handleNewNotification(message);
    });

  
    this.hubConnection
      .start()
      .then(() => console.log('SignalR Notification Hub Connected'))
      .catch(err => console.error('SignalR Connection Error: ', err));
  }

  private handleNewNotification(message: string) {
    // Try to parse structured payloads (server may send JSON with target info)
    let payload: any = null;
    try {
      payload = JSON.parse(message);
    } catch {
      payload = null;
    }

    const currentEmail = String(this.auth.currentUser()?.email || '').toLowerCase();

    // Determine recipient / broadcast
    const isBroadcast = payload?.broadcast === true || payload?.target === 'all' || payload?.to === 'all';
    const targetEmail = (payload?.recipientEmail || payload?.userEmail || payload?.email || payload?.toEmail || payload?.targetEmail) ? String(payload?.recipientEmail || payload?.userEmail || payload?.email || payload?.toEmail || payload?.targetEmail).toLowerCase() : null;

    // If payload contains a target and it doesn't match current user, ignore it
    if (!isBroadcast && targetEmail && targetEmail !== currentEmail) {
      return; // not for this user
    }

    // Build message text
    const text = payload?.message || payload?.text || payload?.body || message;

    this.snackBar.open(text, 'OK', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top'
    });

    const newNotif = {
      message: text,
      date: new Date(),
      read: false,
      raw: payload ?? null
    };

    this.notifications.update(prev => [newNotif, ...prev]);
    this.unreadCount.update(count => count + 1);
  }

  clearNotifications() {
    this.notifications.set([]);
    this.unreadCount.set(0);
  }

  markAsRead(notif: any) {
    
    this.notifications.update(prev => prev.map(n => n === notif ? { ...n, read: true } : n));
   
    const unread = this.notifications().filter(n => !n.read).length;
    this.unreadCount.set(unread);
  }

  markAllRead() {
    this.notifications.update(prev => prev.map(n => ({ ...n, read: true })));
    this.unreadCount.set(0);
  }

  // Public helper to push a local notification (useful when the client wants
  // to show a notification immediately after an action, without waiting for
  // a server-side SignalR push).
  notify(message: string) {
    if (!message) return;
    // Allow structured messages to be passed as objects or strings
    if (typeof message === 'object') {
      try { this.handleNewNotification(JSON.stringify(message)); } catch { this.handleNewNotification(String(message)); }
      return;
    }
    this.handleNewNotification(message);
  }
}
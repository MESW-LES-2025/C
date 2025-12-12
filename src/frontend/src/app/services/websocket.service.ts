import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Message {
    id: number;
    senderId: string;
    senderName: string;
    recipientId: string;
    recipientName: string;
    processId: string;
    subject: string;
    body: string;
    createdAt: Date;
    readAt?: Date;
}

@Injectable({
    providedIn: 'root'
})
export class WebsocketService {
    private hubConnection: signalR.HubConnection | undefined;
    private messageReceivedSubject = new BehaviorSubject<Message | null>(null);
    public messageReceived$ = this.messageReceivedSubject.asObservable();

    constructor() { }

    public startConnection(token: string): void {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(environment.apiBaseUrl.replace('/api', '') + '/messageHub', {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection
            .start()
            .then(() => console.log('Connection started'))
            .catch(err => console.log('Error while starting connection: ' + err));

        this.hubConnection.on('ReceiveMessage', (data: Message) => {
            this.messageReceivedSubject.next(data);
        });

        this.hubConnection.on('MessageSent', (data: Message) => {
            this.messageReceivedSubject.next(data);
        });
    }

    public stopConnection(): void {
        if (this.hubConnection) {
            this.hubConnection.stop();
        }
    }

    public sendMessage(recipientId: string, processId: string, subject: string, body: string): Promise<void> {
        if (this.hubConnection) {
            return this.hubConnection.invoke('SendMessage', { recipientId, processId, subject, body });
        }
        return Promise.reject('Connection not started');
    }
}

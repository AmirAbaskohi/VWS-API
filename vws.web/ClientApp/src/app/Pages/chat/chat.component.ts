import { Component, OnInit } from '@angular/core';
import {HubConnection, HubConnectionBuilder} from '@microsoft/signalr';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit {

  hubConnection: HubConnection;
  messages: Array<string> = [];
  newMessage: string;

  constructor() { }

  ngOnInit() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:44303/chatHub')
      .build();
    this.hubConnection.on('ReciveMessage', (message: string) => {
      this.messages.push(message);
    });

    this.hubConnection.start();
  }

  sendMessageToAll() {
    this.hubConnection.send('SendMessage', this.newMessage);
    this.newMessage = '';
  }

}

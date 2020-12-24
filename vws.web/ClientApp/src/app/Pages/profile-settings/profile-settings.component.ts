import { Component, OnInit } from '@angular/core';
import {FormControl} from '@angular/forms';
import {Observable} from 'rxjs';
import {map, startWith} from 'rxjs/operators';

@Component({
  selector: 'app-profile-settings',
  templateUrl: './profile-settings.component.html',
  styleUrls: ['./profile-settings.component.scss']
})
export class ProfileSettingsComponent implements OnInit {

  myControl = new FormControl();
  options: string[] = [' US/Alaska ', ' Asia/Tehran ', ' Africa/Lusaka ', ' America/Boa Vista', ' Asia/Krasnoyarsk ', ' America/Chicago', ' Europe/Brussels', ' Pacific/Truk '];
  filteredOptions: Observable<string[]>;

  hide = true;

  userMainTheme = '#7b68ee';
  colorSlideToggle = '#7b68ee';

  ImportExport = false;
  Trash = false;
  MySettings = true;
  Workspaces = false;
  Notifications = false;
  Rewards = false;
  LogOut = false;
  CloudStorage = false;
  TimeTracking = false;
  SlackNotifications = false;
  Apps = false;
  Calendar = false;
  Zoom = false;

  constructor() { }

  ngOnInit() {
    this.filteredOptions = this.myControl.valueChanges
      .pipe(
        startWith(''),
        map(value => this._filter(value))
      );
  }

  private _filter(value: string): string[] {
    const filterValue = value.toLowerCase();

    return this.options.filter(option => option.toLowerCase().includes(filterValue));
  }

  selectTheme(id): void {
    this.userMainTheme = '#' + id;
    this.colorSlideToggle = '#' + id;
  }

  selectMenuItem(id): void {
    // tslint:disable-next-line:triple-equals
    if (id == 'ImportExport') {
      this.ImportExport = true;
    } else {
      this.ImportExport = false;
    }

    // tslint:disable-next-line:triple-equals
    if (id == 'Trash') {
      this.Trash = true;
    } else {
      this.Trash = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'MySettings') {
      this.MySettings = true;
    } else {
      this.MySettings = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Workspaces') {
      this.Workspaces = true;
    } else {
      this.Workspaces = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Notifications') {
      this.Notifications = true;
    } else {
      this.Notifications = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Rewards') {
      this.Rewards = true;
    } else {
      this.Rewards = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'LogOut') {
      this.LogOut = true;
    } else {
      this.LogOut = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'CloudStorage') {
      this.CloudStorage = true;
    } else {
      this.CloudStorage = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'TimeTracking') {
      this.TimeTracking = true;
    } else {
      this.TimeTracking = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'SlackNotifications') {
      this.SlackNotifications = true;
    } else {
      this.SlackNotifications = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Apps') {
      this.Apps = true;
    } else {
      this.Apps = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Calendar') {
      this.Calendar = true;
    } else {
      this.Calendar = false;
    }
    // tslint:disable-next-line:triple-equals
    if (id == 'Zoom') {
      this.Zoom = true;
    } else {
      this.Zoom = false;
    }

  }

}

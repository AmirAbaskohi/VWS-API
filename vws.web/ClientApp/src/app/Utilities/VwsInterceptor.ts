import {HttpEvent, HttpHandler, HttpInterceptor, HttpRequest} from '@angular/common/http';
import {Observable} from 'rxjs';
import {DomainName} from './PathTools';

export class VwsInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // let myRequest: HttpRequest<any> = req;
    const myRequest = req.clone({url: DomainName + req.url});
    return next.handle(myRequest);
  }

}

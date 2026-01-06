import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BillingService } from '../../services/billing-service'; // Adjust path
import { AuthService } from '../../services/auth-service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatSnackBarModule,
    MatTableModule,
    MatPaginatorModule
  ],
  templateUrl: './billing.html',
  styleUrl: './billing.css',
})
export class Billing implements OnInit {
  private billingService = inject(BillingService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  invoices = signal<any[]>([]); 
  // payment dialog state
  showPaymentDialog = signal<boolean>(false);
  paymentInvoiceId: number | null = null;
  paymentMethod: string | null = null;
  paymentMethods = ['Cash', 'UPI', 'Card'];
  ngOnInit() {
    this.loadInvoices();
  }

  loadInvoices() {
    const userId = this.authService.currentUser()?.email || ''; 
    const role = this.authService.userRole();

    this.billingService.getInvoicesAsync(userId, role).subscribe({
      next: (res) => this.invoices.set(res),
      error: (err) => console.error('Error fetching invoices:', err)
    });
  }

  payInvoice(invoiceId: number) {
    // open payment options dialog
    this.paymentInvoiceId = invoiceId;
    this.paymentMethod = null;
    this.showPaymentDialog.set(true);
  }

  confirmPayment() {
    if (!this.paymentInvoiceId) return;
    const method = this.paymentMethod ?? 'Unknown';
    this.billingService.payInvoiceAsync(this.paymentInvoiceId, method).subscribe({
      next: () => {
        this.snackBar.open('Payment Successful! ✅', 'Close', { duration: 3000 });
        this.closePaymentDialog();
        this.loadInvoices();
      },
      error: (err) => {
        this.snackBar.open('Payment failed. Please try again.', 'Close', { duration: 3000 });
        console.error(err);
      }
    });
  }

  closePaymentDialog() {
    this.showPaymentDialog.set(false);
    this.paymentInvoiceId = null;
    this.paymentMethod = null;
  }
}
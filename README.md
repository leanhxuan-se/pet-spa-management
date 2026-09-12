# 🐾 Pet Spa Management System

A web-based management system for pet spa businesses, designed to help staff manage customers, pets, appointments, services, employees, products, inventory, and payments in one centralized system.

## 📌 Overview

The **Pet Spa Management System** aims to digitize the daily operations of a pet spa.

The system provides tools for:

- Customer and pet management
- Appointment management
- Service management
- Employee management
- Product and inventory management
- Invoice and payment management
- Voucher and promotion management
- Revenue and business reporting

## 🎯 Project Goals

- Reduce manual management work.
- Centralize customer and pet information.
- Improve appointment scheduling.
- Help employees manage daily operations efficiently.
- Track services, products, payments, and revenue.
- Provide useful reports for business management.

## 🚀 Main Features

### Customer & Pet Management

- Create, update, and manage customer information.
- Manage pet profiles.
- Track pet service history.
- Store notes and important information about pets.

### Appointment Management

- Create and manage appointments.
- Check appointment status.
- Assign employees to appointments.
- Manage cancellations and rescheduling.
- Prevent scheduling conflicts.

### Service Management

- Create and manage spa services.
- Configure service prices.
- Configure service duration.
- Track services performed for each pet.

### Employee Management

- Manage employee information.
- Manage employee roles and permissions.
- Manage work schedules.
- Assign employees to services.

### Product & Inventory

- Manage products.
- Track inventory quantity.
- Manage suppliers.
- Record stock-in and stock-out operations.

### Invoice & Payment

- Generate invoices.
- Calculate service and product costs.
- Apply discounts and vouchers.
- Record payment status and payment methods.

### Reports

- Revenue reports.
- Appointment statistics.
- Popular services.
- Product sales.
- Customer statistics.

## 🏗️ System Architecture

## 🛠️ Technologies

### Frontend

### Backend

### Development Tools

- Git
- GitHub

## 📂 Project Structure

## ⚙️ Installation

### 1. Clone repository

```bash
git clone <repository-url>
cd pet-spa-management
```

### 2. Install dependencies

Backend:

```bash
cd server
npm install
```

Frontend:

```bash
cd client
npm install
```

### 3. Configure environment variables

Create a `.env` file in the backend directory:

```env
PORT=5000

MONGODB_URL=<your-mongodb-url>
DATABASE_NAME=pet_spa

JWT_ACCESS_SECRET=<your-access-secret>
JWT_ACCESS_EXPIRES_IN=15m
```

> Do not commit `.env` files or other sensitive credentials to GitHub.

### 4. Run the project

Backend:

```bash
npm run dev
```

Frontend:

```bash
npm run dev
```

## 📋 Core Business Workflow

## 👥 Team

## 📌 Project Status

## 📄 Documentation

Project documentation is stored in the `docs/` directory.

```text
docs/
├── requirements/
├── api/
├── database/
└── diagrams/
```

## 🤝 Contributing

1. Create a new branch from `develop`.

```bash
git checkout develop
git pull
git checkout -b feature/<feature-name>
```

2. Implement your feature.

3. Commit your changes.

```bash
git add .
git commit -m "feat: add appointment management"
```

4. Push the branch.

```bash
git push origin feature/<feature-name>
```

5. Create a Pull Request into `develop`.

## 📜 License

This project is developed for educational purposes.

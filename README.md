# Contract Monthly Claim System (CMCS) 

## Project Overview
The Contract Monthly Claim System (CMCS) is a comprehensive web-based MVC application designed to streamline the monthly claim submission and approval process for Independent Contractor (IC) lecturers. This system represents a transformative approach to administrative processes in academic institutions, offering a seamless platform for claim management from submission through to final approval and payment.

## User Roles & System Workflow

### 1. Lecturer Role
**Primary Functions:**
- Submit monthly claims with automated calculations
- Upload supporting documents (PDF, DOCX, XLSX)
- Track claim status through visual progress indicators
- View personal claim history and statistics

**Key Features:**
- Automatic user identification via session management
- Pre-populated hourly rates set by HR
- Real-time total amount calculation
- Hours validation (0.5-180 hours per month)
- Secure document upload with encryption

### 2. Programme Coordinator Role
**Primary Functions:**
- Review submitted claims from lecturers
- Approve or reject claims with comments
- Monitor claim workflow progress
- Access coordinator-specific dashboard

### 3. Academic Manager Role
**Primary Functions:**
- Final approval authority for claims
- Oversee entire claim workflow
- Access management reports and analytics
- Make final decisions on claim payments

### 4. HR Administrator Role
**Primary Functions:**
- User management and profile creation
- Set and update lecturer hourly rates
- Generate system reports and analytics
- System administration and oversight

## Technical Implementation

### Authentication & Security
- **Custom Authentication System**: Session-based login with password hashing using SHA256 and unique salts
- **Role-Based Access Control**: Authorization filters preventing unauthorized page access
- **Session Management**: Automatic timeout (2 hours) with secure cookie settings
- **Data Encryption**: AES encryption for stored document files

### Database Architecture
- **SQLite Database** with Entity Framework Core code-first approach
- **Complete Data Migration** from JSON files to relational database
- **Automatic ID Generation** with proper foreign key relationships
- **Seeded Initial Data** including sample users, claims, and status workflows

### Key Technical Features
- **Repository Pattern** with IDataService interface for data abstraction
- **Dependency Injection** throughout the application
- **Entity Framework Migrations** for database schema management
- **Professional Error Handling** with user-friendly messages

### Authentication System
✅ **Custom Login System** with session management  
✅ **Password Security** using SHA256 hashing with unique salts  
✅ **Role-Based Authorization** preventing unauthorized access  
✅ **Session Validation** across all protected controllers  

### Database Integration
✅ **Full SQLite Implementation** with Entity Framework Core  
✅ **Database Migrations** for proper schema management  
✅ **Encrypted Document Storage** maintaining file security  
✅ **Relational Data Model** with proper foreign key constraints  

### User Experience Enhancements
✅ **Automatic User Recognition** - lecturers no longer manually select identity once logged in 
✅ **Pre-filled Hourly Rates** from HR-defined user profiles  
✅ **Real-time Calculations** as hours are entered in forms  
✅ **Role-Based Navigation** with appropriate dashboard redirection  

### Claim Management Improvements
✅ **Simplified Submission Process** - removed user selection dropdown  
✅ **Enhanced Validation** for hours worked and file uploads  
✅ **Improved Error Handling** with contextual error messages  
✅ **Advanced Form UX** with character counters and file previews  

### HR System Implementation
✅ **User Management** - HR can create and manage all user accounts  
✅ **Rate Management** - Set and update lecturer hourly rates  
✅ **Report Generation** - System analytics and reporting capabilities  
✅ **Administrative Oversight** - Complete system administration access  

## Database Schema

### Users Table
- 'userId' (Primary Key, Auto-increment)
- 'firstName', 'lastName', 'email', 'phoneNumber'
- 'userRole' (Lecturer/Coordinator/Manager/HR)
- 'hourlyRate' (Decimal, set by HR)
- 'passwordHash', 'passwordSalt' (Authentication)

### Claims Table
- 'claimId' (Primary Key, Auto-increment)
- 'userId' (Foreign Key)
- 'hoursWorked', 'hourlyRate', 'totalAmount' (Decimal)
- 'statusId' (Foreign Key)
- 'submissionDate' (DateTime)
- 'Notes' (Optional comments)

### Documents Table
- 'documentId' (Primary Key, Auto-increment)
- 'claimId' (Foreign Key)
- 'fileName', 'fileType', 'fileSize'
- 'uploadDate' (DateTime)

### ClaimStatuses Table
- 'statusId' (Primary Key)
- 'statusName' (Submitted/Approved by Coordinator/Approved by Manager/Rejected/Paid)

## Default Login Credentials

**Lecturer Accounts:**
- Email: 'mattjones@university.co.za' | Password: 'password123'
- Email: 'crownvic@university.co.za' | Password: 'password123'

**Coordinator Account:**
- Email: 'sarahw@university.co.za' | Password: 'password123'

**Manager Account:**
- Email: 'davidb@university.co.za' | Password: 'password123'

**HR Administrator:**
- Email: 'hr@university.co.za' | Password: 'admin123'

## Installation & Setup

### Prerequisites
- .NET 8.0 SDK
- SQLite (included with EF Core)

### Installation Steps
1. Clone the repository to your local machine
2. Navigate to the project directory
3. Run database migrations in package manager console:
   Add-Migration InitialCreate (Only run this if no migrations exist)
   Update-Database
   Remove-Migration (only run if you want to remove the existing migrations)
   

-- MariaDB dump 10.19  Distrib 10.4.32-MariaDB, for Win64 (AMD64)
--
-- Host: localhost    Database: registrar_db
-- ------------------------------------------------------
-- Server version	10.4.32-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `registrar_db`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `registrar_db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;

USE `registrar_db`;

--
-- Table structure for table `activity_log`
--

DROP TABLE IF EXISTS `activity_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `activity_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user_id` int(11) DEFAULT NULL,
  `message` text NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `activity_log`
--

LOCK TABLES `activity_log` WRITE;
/*!40000 ALTER TABLE `activity_log` DISABLE KEYS */;
INSERT INTO `activity_log` VALUES (1,1,'System database initialized with college curriculum and multi-role records.','2026-09-22 02:35:08'),(2,5,'Enrollment approved for AY 2025-2026 1st Semester by Registrar.','2026-09-22 02:35:08'),(3,13,'CHED Special Order No. 04-2025-10492 issued for BSIT graduation.','2026-09-22 02:35:08'),(4,15,'Transferee subject credits accredited (CS101, ENG101) by Registrar.','2026-09-22 02:35:08'),(5,11,'New student 201 File created and verified.','2026-09-22 02:35:08');
/*!40000 ALTER TABLE `activity_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `add_drop_requests`
--

DROP TABLE IF EXISTS `add_drop_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `add_drop_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `enrollment_id` int(11) NOT NULL,
  `student_id` int(11) NOT NULL,
  `request_type` enum('add','drop','change') NOT NULL,
  `class_offering_id` int(11) DEFAULT NULL,
  `target_class_offering_id` int(11) DEFAULT NULL,
  `reason` text DEFAULT NULL,
  `status` enum('pending','approved','rejected') NOT NULL DEFAULT 'pending',
  `requested_at` datetime NOT NULL DEFAULT current_timestamp(),
  `processed_at` datetime DEFAULT NULL,
  `processed_by` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `add_drop_requests_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `add_drop_requests`
--

LOCK TABLES `add_drop_requests` WRITE;
/*!40000 ALTER TABLE `add_drop_requests` DISABLE KEYS */;
INSERT INTO `add_drop_requests` VALUES (1,3,7,'change',9,10,'Conflict with student organization executive meeting schedule on Wednesday afternoon.','pending','2025-08-25 10:15:00',NULL,NULL);
/*!40000 ALTER TABLE `add_drop_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `api_tokens`
--

DROP TABLE IF EXISTS `api_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `api_tokens` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `system_name` varchar(100) NOT NULL,
  `token` varchar(64) NOT NULL,
  `permissions` varchar(255) NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `token` (`token`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `api_tokens`
--

LOCK TABLES `api_tokens` WRITE;
/*!40000 ALTER TABLE `api_tokens` DISABLE KEYS */;
INSERT INTO `api_tokens` VALUES (1,'Student Portal Mobile/Web','stu_live_token_77a912e8b4039f','student_read,document_request',1,'2026-09-22 02:35:08'),(2,'Faculty Portal LMS Sync','fac_live_token_88c4210d6e112a','faculty_read,grades_write,class_roster',1,'2026-09-22 02:35:08'),(3,'Guidance & Counseling System','gui_live_token_99f338a11b554c','guidance_read,clearance_write,risk_audit',1,'2026-09-22 02:35:08');
/*!40000 ALTER TABLE `api_tokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ched_special_orders`
--

DROP TABLE IF EXISTS `ched_special_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ched_special_orders` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `so_number` varchar(100) NOT NULL,
  `series_year` varchar(20) NOT NULL,
  `program` varchar(100) NOT NULL,
  `date_applied` date NOT NULL,
  `date_issued` date DEFAULT NULL,
  `status` enum('Applied','Issued','Under Review','Pending Requirements') NOT NULL DEFAULT 'Applied',
  `remarks` text DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `ched_special_orders_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ched_special_orders`
--

LOCK TABLES `ched_special_orders` WRITE;
/*!40000 ALTER TABLE `ched_special_orders` DISABLE KEYS */;
INSERT INTO `ched_special_orders` VALUES (1,13,'SO (B) No. 04-2025-10492','2025','BS Information Technology','2025-06-15','2025-08-01','Issued','Special Order successfully approved by CHED NCR'),(2,5,'SO (B) No. 04-2025-10493','2025','BS Information Technology','2025-07-20',NULL,'Under Review','Submitted to CHED Regional Office for batch endorsement'),(3,6,'SO (B) No. 04-2025-10494','2025','BS Computer Science','2025-07-20',NULL,'Applied','Candidate documents undergoing final registrar clearance');
/*!40000 ALTER TABLE `ched_special_orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `class_offerings`
--

DROP TABLE IF EXISTS `class_offerings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `class_offerings` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `subject_id` int(11) NOT NULL,
  `section_code` varchar(50) NOT NULL,
  `school_year` varchar(20) NOT NULL,
  `semester` varchar(20) NOT NULL,
  `room` varchar(50) NOT NULL,
  `days_of_week` varchar(50) NOT NULL,
  `start_time` time NOT NULL,
  `end_time` time NOT NULL,
  `instructor_name` varchar(100) NOT NULL DEFAULT 'Prof. TBD',
  `capacity` int(11) NOT NULL DEFAULT 40,
  `slots_taken` int(11) NOT NULL DEFAULT 0,
  `status` enum('open','closed','cancelled') NOT NULL DEFAULT 'open',
  PRIMARY KEY (`id`),
  KEY `subject_id` (`subject_id`),
  CONSTRAINT `class_offerings_ibfk_1` FOREIGN KEY (`subject_id`) REFERENCES `subjects` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `class_offerings`
--

LOCK TABLES `class_offerings` WRITE;
/*!40000 ALTER TABLE `class_offerings` DISABLE KEYS */;
INSERT INTO `class_offerings` VALUES (1,1,'BSIT-1A','2025-2026','1st Semester','CL-301','Mon,Wed','08:00:00','09:30:00','Prof. Maria Victoria Cruz',40,35,'open'),(2,2,'BSIT-1A','2025-2026','1st Semester','IT-LAB-1','Tue,Thu','10:00:00','12:30:00','Engr. Danilo Castillo',35,35,'closed'),(3,3,'BSIT-1A','2025-2026','1st Semester','RM-204','Fri','08:00:00','11:00:00','Prof. Roberto Ramos',45,38,'open'),(4,4,'BSIT-1A','2025-2026','1st Semester','RM-205','Mon,Wed','13:00:00','14:30:00','Dr. Elena Valenzuela',40,36,'open'),(5,5,'BSIT-1A','2025-2026','1st Semester','GYM-A','Sat','08:00:00','11:00:00','Coach Jeffrey Santos',50,42,'open'),(6,12,'BSIT-2A','2025-2026','1st Semester','IT-LAB-2','Mon,Wed','10:00:00','12:30:00','Engr. Danilo Castillo',35,30,'open'),(7,13,'BSIT-2A','2025-2026','1st Semester','IT-LAB-3','Tue,Thu','13:00:00','15:30:00','Prof. Maria Victoria Cruz',35,29,'open'),(8,14,'BSIT-2A','2025-2026','1st Semester','RM-302','Fri','13:00:00','16:00:00','Dr. Elena Valenzuela',40,28,'open'),(9,18,'BSIT-3A','2025-2026','1st Semester','CL-305','Mon,Wed','14:00:00','15:30:00','Engr. Danilo Castillo',40,26,'open'),(10,19,'BSIT-3A','2025-2026','1st Semester','IT-LAB-2','Tue,Thu','08:00:00','10:30:00','Prof. Allan Gomez',35,26,'open'),(11,20,'BSIT-3A','2025-2026','1st Semester','IT-LAB-1','Fri','08:00:00','13:00:00','Prof. Maria Victoria Cruz',35,25,'open'),(12,23,'BSIT-4A','2025-2026','1st Semester','IT-RES-ROOM','Wed','13:00:00','17:00:00','Dr. Rosalinda Santos',30,22,'open'),(13,24,'BSIT-4A','2025-2026','1st Semester','OJT-COOR','Sat','09:00:00','12:00:00','Prof. Roberto Ramos',40,24,'open'),(14,25,'BSIT-4A','2025-2026','1st Semester','AVR-1','Fri','14:00:00','17:00:00','Dr. Rosalinda Santos',50,24,'open');
/*!40000 ALTER TABLE `class_offerings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `completion_revision_requests`
--

DROP TABLE IF EXISTS `completion_revision_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `completion_revision_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `grade_id` int(11) NOT NULL,
  `student_id` int(11) NOT NULL,
  `request_type` enum('completion','revision') NOT NULL,
  `requested_grade` decimal(3,2) NOT NULL,
  `reason` text DEFAULT NULL,
  `status` enum('pending','approved','rejected') NOT NULL DEFAULT 'pending',
  `requested_at` datetime NOT NULL DEFAULT current_timestamp(),
  `processed_at` datetime DEFAULT NULL,
  `processed_by` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `grade_id` (`grade_id`),
  CONSTRAINT `completion_revision_requests_ibfk_1` FOREIGN KEY (`grade_id`) REFERENCES `grades` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `completion_revision_requests`
--

LOCK TABLES `completion_revision_requests` WRITE;
/*!40000 ALTER TABLE `completion_revision_requests` DISABLE KEYS */;
INSERT INTO `completion_revision_requests` VALUES (1,1,5,'revision',1.00,'Faculty computation adjustment: missed major project score was submitted and credited with approval of Department Chair.','pending','2025-08-27 16:30:00',NULL,NULL);
/*!40000 ALTER TABLE `completion_revision_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `course_shifting_requests`
--

DROP TABLE IF EXISTS `course_shifting_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `course_shifting_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `from_program` varchar(100) NOT NULL,
  `to_program` varchar(100) NOT NULL,
  `reason` text DEFAULT NULL,
  `status` enum('pending','approved','rejected') NOT NULL DEFAULT 'pending',
  `requested_at` datetime NOT NULL DEFAULT current_timestamp(),
  `approved_at` datetime DEFAULT NULL,
  `evaluated_by` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `course_shifting_requests_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `course_shifting_requests`
--

LOCK TABLES `course_shifting_requests` WRITE;
/*!40000 ALTER TABLE `course_shifting_requests` DISABLE KEYS */;
INSERT INTO `course_shifting_requests` VALUES (1,10,'BS Computer Science','BS Information Technology','Career interest alignment with web/cloud infrastructure and enterprise network systems.','pending','2025-08-24 13:45:00',NULL,NULL);
/*!40000 ALTER TABLE `course_shifting_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `document_credentials`
--

DROP TABLE IF EXISTS `document_credentials`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `document_credentials` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `document_type` varchar(100) NOT NULL,
  `status` enum('Missing','Submitted','Verified') NOT NULL DEFAULT 'Missing',
  `remarks` text DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `submitted_at` datetime DEFAULT NULL,
  `verified_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `stud_doc` (`student_id`,`document_type`),
  CONSTRAINT `document_credentials_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=73 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `document_credentials`
--

LOCK TABLES `document_credentials` WRITE;
/*!40000 ALTER TABLE `document_credentials` DISABLE KEYS */;
INSERT INTO `document_credentials` VALUES (1,5,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(2,5,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(3,5,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(4,5,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(5,5,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(6,5,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(7,6,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(8,6,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(9,6,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(10,6,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(11,6,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(12,6,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(13,7,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(14,7,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(15,7,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(16,7,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(17,7,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(18,7,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(19,8,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(20,8,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(21,8,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(22,8,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(23,8,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(24,8,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(25,9,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(26,9,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(27,9,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(28,9,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(29,9,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(30,9,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(31,10,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(32,10,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(33,10,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(34,10,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(35,10,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(36,10,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(37,11,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(38,11,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(39,11,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(40,11,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(41,11,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(42,11,'Medical Clearance','Submitted','Awaiting university clinic doctor signature',NULL,'2024-08-15 10:00:00',NULL),(43,12,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(44,12,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(45,12,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(46,12,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(47,12,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(48,12,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(49,13,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(50,13,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(51,13,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(52,13,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(53,13,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(54,13,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(55,14,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(56,14,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(57,14,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(58,14,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(59,14,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(60,14,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(61,15,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(62,15,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(63,15,'Birth Certificate','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(64,15,'Good Moral','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(65,15,'Transcript from Previous School','Verified','Official TOR with seal received directly from previous institution',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(66,15,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(67,16,'Form 137','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(68,16,'Form 138','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(69,16,'Birth Certificate','Missing','Deficiency notice sent to student',NULL,NULL,NULL),(70,16,'Good Moral','Missing','Deficiency notice sent to student',NULL,NULL,NULL),(71,16,'Transcript from Previous School','Verified','Not applicable (Direct High School Graduate entry)',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00'),(72,16,'Medical Clearance','Verified','Official authenticated copy verified by registrar.',NULL,'2024-08-15 10:00:00','2024-08-20 14:30:00');
/*!40000 ALTER TABLE `document_credentials` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `enrolled_subjects`
--

DROP TABLE IF EXISTS `enrolled_subjects`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `enrolled_subjects` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `enrollment_id` int(11) NOT NULL,
  `class_offering_id` int(11) NOT NULL,
  `status` enum('enrolled','dropped','passed','failed','inc') NOT NULL DEFAULT 'enrolled',
  PRIMARY KEY (`id`),
  KEY `enrollment_id` (`enrollment_id`),
  KEY `class_offering_id` (`class_offering_id`),
  CONSTRAINT `enrolled_subjects_ibfk_1` FOREIGN KEY (`enrollment_id`) REFERENCES `enrollments` (`id`) ON DELETE CASCADE,
  CONSTRAINT `enrolled_subjects_ibfk_2` FOREIGN KEY (`class_offering_id`) REFERENCES `class_offerings` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `enrolled_subjects`
--

LOCK TABLES `enrolled_subjects` WRITE;
/*!40000 ALTER TABLE `enrolled_subjects` DISABLE KEYS */;
INSERT INTO `enrolled_subjects` VALUES (1,1,12,'enrolled'),(2,1,13,'enrolled'),(3,1,14,'enrolled'),(4,4,1,'enrolled'),(5,4,2,'enrolled'),(6,4,3,'enrolled'),(7,4,4,'enrolled'),(8,4,5,'enrolled'),(9,5,12,'enrolled'),(10,5,13,'enrolled'),(11,5,14,'enrolled'),(12,3,9,'enrolled'),(13,3,10,'enrolled'),(14,3,11,'enrolled');
/*!40000 ALTER TABLE `enrolled_subjects` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `enrollments`
--

DROP TABLE IF EXISTS `enrollments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `enrollments` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `school_year` varchar(20) NOT NULL,
  `semester` varchar(20) NOT NULL,
  `enrollment_type` enum('new','old','transferee','cross-enrollee','returnee') NOT NULL DEFAULT 'old',
  `status` enum('pending','active','rejected','cancelled') NOT NULL DEFAULT 'pending',
  `total_units` decimal(4,1) NOT NULL DEFAULT 0.0,
  `enrolled_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `enrollments_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `enrollments`
--

LOCK TABLES `enrollments` WRITE;
/*!40000 ALTER TABLE `enrollments` DISABLE KEYS */;
INSERT INTO `enrollments` VALUES (1,5,'2025-2026','1st Semester','old','active',12.0,'2025-08-10 09:15:00'),(2,6,'2025-2026','1st Semester','old','active',12.0,'2025-08-11 11:30:00'),(3,7,'2025-2026','1st Semester','old','active',21.0,'2025-08-12 14:00:00'),(4,11,'2025-2026','1st Semester','new','active',20.0,'2025-08-14 08:30:00'),(5,13,'2025-2026','1st Semester','old','active',12.0,'2025-08-10 10:00:00'),(6,15,'2025-2026','1st Semester','transferee','active',18.0,'2025-08-15 13:00:00'),(7,16,'2025-2026','1st Semester','old','pending',15.0,'2025-08-20 16:45:00'),(8,12,'2025-2026','1st Semester','new','pending',18.0,'2025-08-21 09:20:00');
/*!40000 ALTER TABLE `enrollments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `grades`
--

DROP TABLE IF EXISTS `grades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `grades` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `enrolled_subject_id` int(11) NOT NULL,
  `grade` decimal(3,2) DEFAULT NULL,
  `is_inc` tinyint(1) NOT NULL DEFAULT 0,
  `status` enum('draft','submitted','verified','locked') NOT NULL DEFAULT 'draft',
  `remarks` varchar(100) DEFAULT NULL,
  `encoded_by` int(11) DEFAULT NULL,
  `encoded_at` datetime DEFAULT NULL,
  `verified_by` int(11) DEFAULT NULL,
  `verified_at` datetime DEFAULT NULL,
  `locked_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `enrolled_subject_id` (`enrolled_subject_id`),
  CONSTRAINT `grades_ibfk_1` FOREIGN KEY (`enrolled_subject_id`) REFERENCES `enrolled_subjects` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `grades`
--

LOCK TABLES `grades` WRITE;
/*!40000 ALTER TABLE `grades` DISABLE KEYS */;
INSERT INTO `grades` VALUES (1,1,1.25,0,'verified','Passed - Excellent',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL),(2,2,1.00,0,'verified','Passed - Excellent',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL),(3,3,1.25,0,'verified','Passed - Excellent',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL),(4,9,1.00,0,'locked','Passed - Superior',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL),(5,10,1.00,0,'locked','Passed - Superior',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL),(6,11,1.00,0,'locked','Passed - Superior',1,'2026-09-22 02:35:08',1,'2026-09-22 02:35:08',NULL);
/*!40000 ALTER TABLE `grades` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `nstp_serial_numbers`
--

DROP TABLE IF EXISTS `nstp_serial_numbers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `nstp_serial_numbers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `nstp_component` enum('CWTS','ROTC','LTS') NOT NULL DEFAULT 'CWTS',
  `serial_number` varchar(100) NOT NULL,
  `date_issued` date NOT NULL,
  `remarks` text DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `serial_number` (`serial_number`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `nstp_serial_numbers_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `nstp_serial_numbers`
--

LOCK TABLES `nstp_serial_numbers` WRITE;
/*!40000 ALTER TABLE `nstp_serial_numbers` DISABLE KEYS */;
INSERT INTO `nstp_serial_numbers` VALUES (1,5,'CWTS','NSTP-CWTS-2023-NCR-09412','2023-06-15','Official DND/CHED certified completion serial'),(2,6,'ROTC','NSTP-ROTC-2023-NCR-04192','2023-06-15','Commissioned / Certified basic ROTC cadet graduate'),(3,13,'CWTS','NSTP-CWTS-2023-NCR-09415','2023-06-15','Official DND/CHED certified completion serial'),(4,7,'CWTS','NSTP-CWTS-2024-NCR-11204','2024-06-20','Official DND/CHED certified completion serial');
/*!40000 ALTER TABLE `nstp_serial_numbers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `overload_waiver_requests`
--

DROP TABLE IF EXISTS `overload_waiver_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `overload_waiver_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `school_year` varchar(20) NOT NULL,
  `semester` varchar(20) NOT NULL,
  `request_type` enum('overload','waiver','cross_enrollment') NOT NULL,
  `requested_units` int(11) NOT NULL DEFAULT 0,
  `reason` text DEFAULT NULL,
  `status` enum('pending','approved','rejected') NOT NULL DEFAULT 'pending',
  `requested_at` datetime NOT NULL DEFAULT current_timestamp(),
  `processed_at` datetime DEFAULT NULL,
  `processed_by` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `overload_waiver_requests_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `overload_waiver_requests`
--

LOCK TABLES `overload_waiver_requests` WRITE;
/*!40000 ALTER TABLE `overload_waiver_requests` DISABLE KEYS */;
INSERT INTO `overload_waiver_requests` VALUES (1,5,'2025-2026','1st Semester','overload',24,'Graduating student in final year; requesting 3-unit overload to complete general elective requirements.','pending','2025-08-26 14:20:00',NULL,NULL);
/*!40000 ALTER TABLE `overload_waiver_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `settings`
--

DROP TABLE IF EXISTS `settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `settings` (
  `key` varchar(50) NOT NULL,
  `value` text NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `settings`
--

LOCK TABLES `settings` WRITE;
/*!40000 ALTER TABLE `settings` DISABLE KEYS */;
INSERT INTO `settings` VALUES ('current_school_year','2025-2026','Active Academic School Year'),('current_semester','1st Semester','Active Academic Semester'),('grade_encoding_deadline','2025-10-30','Deadline for grade encoding'),('grade_encoding_open','1','Flag whether grade encoding is open for faculty/registrar'),('registrar_email','registrar@msu.edu.ph','Official Registrar Email'),('registrar_name','Dr. Rosalinda M. Santos, Ed.D','University Registrar Head'),('school_address','Academic Hub, Taft Avenue, Manila, Philippines','Institution Address'),('school_code','MSU-0422','CHED Institutional Code'),('school_name','Metropolitan State University','Official School/University Name');
/*!40000 ALTER TABLE `settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `student_profile`
--

DROP TABLE IF EXISTS `student_profile`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `student_profile` (
  `user_id` int(11) NOT NULL,
  `program` varchar(100) NOT NULL DEFAULT 'BS Information Technology',
  `year_level` varchar(50) NOT NULL DEFAULT '1st Year',
  `curriculum_year` varchar(20) NOT NULL DEFAULT '2023-2027',
  `average_grade` decimal(4,2) DEFAULT NULL,
  `academic_status` enum('Good Standing','Probation','Graduating','Graduated','Disqualified') NOT NULL DEFAULT 'Good Standing',
  `enrollment_status` enum('Active','On-Leave','Dropped','Dismissed','Graduated') NOT NULL DEFAULT 'Active',
  `completed_units` int(11) NOT NULL DEFAULT 0,
  `units_remaining` int(11) NOT NULL DEFAULT 144,
  `guidance_clearance_status` enum('Cleared','Pending','Flagged') NOT NULL DEFAULT 'Cleared',
  `guidance_notes` text DEFAULT NULL,
  `contact_number` varchar(30) DEFAULT NULL,
  `address` text DEFAULT NULL,
  `birth_date` date DEFAULT NULL,
  `gender` enum('Male','Female') NOT NULL DEFAULT 'Female',
  PRIMARY KEY (`user_id`),
  CONSTRAINT `student_profile_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `student_profile`
--

LOCK TABLES `student_profile` WRITE;
/*!40000 ALTER TABLE `student_profile` DISABLE KEYS */;
INSERT INTO `student_profile` VALUES (5,'BS Information Technology','4th Year','2022-2026',1.28,'Graduating','Active',138,6,'Cleared','Outstanding academic record','09171234567','Quezon City, Metro Manila','2003-05-14','Female'),(6,'BS Computer Science','4th Year','2022-2026',1.48,'Graduating','Active',140,4,'Cleared','No disciplinary issues','09187654321','Manila City','2003-08-20','Male'),(7,'BS Information Technology','3rd Year','2023-2027',1.65,'Good Standing','Active',96,48,'Cleared','Active officer in IT Society','09201122334','Makati City','2004-02-11','Female'),(8,'BS Business Administration','3rd Year','2023-2027',1.82,'Good Standing','Active',90,54,'Cleared','Good standing','09224455667','Pasig City','2004-09-03','Male'),(9,'BS Information Technology','2nd Year','2024-2028',1.70,'Good Standing','Active',52,92,'Cleared','Regular student','09193334455','Taguig City','2005-06-18','Female'),(10,'BS Computer Science','2nd Year','2024-2028',2.10,'Good Standing','Active',48,96,'Cleared','Regular student','09278889900','Mandaluyong City','2005-11-25','Male'),(11,'BS Information Technology','1st Year','2025-2029',1.50,'Good Standing','Active',23,121,'Cleared','Freshman top ranker','09156677889','San Juan City','2006-03-30','Female'),(12,'BS Business Administration','1st Year','2025-2029',1.95,'Good Standing','Active',21,123,'Cleared','Freshman','09169998877','Marikina City','2006-07-12','Male'),(13,'BS Information Technology','4th Year','2022-2026',1.15,'Graduating','Active',142,2,'Cleared','Dean\'s Lister consistently','09285551122','Caloocan City','2003-01-19','Female'),(14,'BS Computer Science','4th Year','2022-2026',1.88,'Graduating','Active',138,6,'Cleared','Eligible for graduation','09172223344','Parañaque City','2003-10-05','Male'),(15,'BS Information Technology','2nd Year','2024-2028',2.05,'Good Standing','Active',45,99,'Cleared','Transferee from FEU Tech','09237776655','Las Piñas City','2004-12-08','Male'),(16,'BS Information Technology','2nd Year','2024-2028',3.12,'Probation','Active',36,108,'Flagged','Referred to guidance due to multiple INC/failed grades','09264443322','Valenzuela City','2005-04-22','Female');
/*!40000 ALTER TABLE `student_profile` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `subjects`
--

DROP TABLE IF EXISTS `subjects`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `subjects` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `subject_code` varchar(30) NOT NULL,
  `subject_name` varchar(150) NOT NULL,
  `units` decimal(3,1) NOT NULL DEFAULT 3.0,
  `lec_hours` int(11) NOT NULL DEFAULT 3,
  `lab_hours` int(11) NOT NULL DEFAULT 0,
  `prerequisite_subject_id` int(11) DEFAULT NULL,
  `curriculum_program` varchar(100) NOT NULL DEFAULT 'BSIT',
  `year_level` varchar(20) NOT NULL DEFAULT '1st Year',
  `semester` varchar(20) NOT NULL DEFAULT '1st Semester',
  PRIMARY KEY (`id`),
  UNIQUE KEY `subject_code` (`subject_code`),
  KEY `prerequisite_subject_id` (`prerequisite_subject_id`),
  CONSTRAINT `subjects_ibfk_1` FOREIGN KEY (`prerequisite_subject_id`) REFERENCES `subjects` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=27 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `subjects`
--

LOCK TABLES `subjects` WRITE;
/*!40000 ALTER TABLE `subjects` DISABLE KEYS */;
INSERT INTO `subjects` VALUES (1,'IT101','Introduction to Computing',3.0,3,0,NULL,'BSIT','1st Year','1st Semester'),(2,'IT102','Computer Programming 1 (Python)',3.0,2,3,NULL,'BSIT','1st Year','1st Semester'),(3,'GE101','Understanding the Self',3.0,3,0,NULL,'All','1st Year','1st Semester'),(4,'MATH101','Mathematics in the Modern World',3.0,3,0,NULL,'All','1st Year','1st Semester'),(5,'NSTP1','National Service Training Program 1',3.0,3,0,NULL,'All','1st Year','1st Semester'),(6,'PE1','Physical Fitness and Wellness',2.0,2,0,NULL,'All','1st Year','1st Semester'),(7,'IT103','Computer Programming 2 (Java & OOP)',3.0,2,3,2,'BSIT','1st Year','2nd Semester'),(8,'IT104','Data Structures and Algorithms',3.0,2,3,2,'BSIT','1st Year','2nd Semester'),(9,'GE102','Purposive Communication',3.0,3,0,NULL,'All','1st Year','2nd Semester'),(10,'NSTP2','National Service Training Program 2',3.0,3,0,5,'All','1st Year','2nd Semester'),(11,'PE2','Rhythmic Activities and Dance',2.0,2,0,6,'All','1st Year','2nd Semester'),(12,'IT201','Information Management & Databases',3.0,2,3,8,'BSIT','2nd Year','1st Semester'),(13,'IT202','Web Systems and Technologies 1',3.0,2,3,7,'BSIT','2nd Year','1st Semester'),(14,'IT203','Discrete Mathematics for IT',3.0,3,0,4,'BSIT','2nd Year','1st Semester'),(15,'GE103','The Contemporary World',3.0,3,0,NULL,'All','2nd Year','1st Semester'),(16,'IT204','Advanced Database Systems',3.0,2,3,12,'BSIT','2nd Year','2nd Semester'),(17,'IT205','Networking and Communications 1',3.0,2,3,1,'BSIT','2nd Year','2nd Semester'),(18,'GE104','Ethics and Professional Conduct',3.0,3,0,NULL,'All','2nd Year','2nd Semester'),(19,'IT301','Systems Analysis and Design',3.0,3,0,12,'BSIT','3rd Year','1st Semester'),(20,'IT302','Information Assurance & Security 1',3.0,2,3,16,'BSIT','3rd Year','1st Semester'),(21,'IT303','Mobile Applications Development',3.0,2,3,13,'BSIT','3rd Year','1st Semester'),(22,'IT304','Software Engineering & Capstone 1',3.0,2,3,18,'BSIT','3rd Year','2nd Semester'),(23,'IT305','Cloud Computing & System Administration',3.0,2,3,16,'BSIT','3rd Year','2nd Semester'),(24,'IT401','Capstone Project 2 (Implementation)',3.0,1,6,21,'BSIT','4th Year','1st Semester'),(25,'IT402','IT Practicum / On-the-Job Training (486 hrs)',6.0,0,18,NULL,'BSIT','4th Year','1st Semester'),(26,'IT403','Seminars and Field Trips in Emerging Tech',3.0,3,0,NULL,'BSIT','4th Year','1st Semester');
/*!40000 ALTER TABLE `subjects` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `system_status`
--

DROP TABLE IF EXISTS `system_status`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `system_status` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `service_name` varchar(100) NOT NULL,
  `status` varchar(50) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `system_status`
--

LOCK TABLES `system_status` WRITE;
/*!40000 ALTER TABLE `system_status` DISABLE KEYS */;
INSERT INTO `system_status` VALUES (1,'Database Node','Operational'),(2,'Enrollment Engine','Online'),(3,'CHED Registry Link','Sync Active'),(4,'Grade Encoding Window','Open'),(5,'Guidance Interop API','Ready'),(6,'Faculty Grade Sync','Connected');
/*!40000 ALTER TABLE `system_status` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transcript_requests`
--

DROP TABLE IF EXISTS `transcript_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `transcript_requests` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `document_type` varchar(100) NOT NULL,
  `purpose` text DEFAULT NULL,
  `copies` int(11) NOT NULL DEFAULT 1,
  `status` enum('pending','processing','ready','released','rejected') NOT NULL DEFAULT 'pending',
  `qr_code_token` varchar(64) NOT NULL,
  `requested_at` datetime NOT NULL DEFAULT current_timestamp(),
  `processed_at` datetime DEFAULT NULL,
  `released_at` datetime DEFAULT NULL,
  `remarks` text DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `qr_code_token` (`qr_code_token`),
  KEY `student_id` (`student_id`),
  CONSTRAINT `transcript_requests_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transcript_requests`
--

LOCK TABLES `transcript_requests` WRITE;
/*!40000 ALTER TABLE `transcript_requests` DISABLE KEYS */;
INSERT INTO `transcript_requests` VALUES (1,5,'Official Transcript of Records (TOR)','Employment & Board Exam application at PRC',2,'processing','QR-TOR-2025-09812A','2025-08-20 11:00:00',NULL,NULL,NULL),(2,13,'Certificate of Good Moral Character','Scholarship & Graduate School Admission',1,'ready','QR-GMC-2025-11044B','2025-08-22 09:30:00',NULL,NULL,NULL),(3,7,'Certificate of Grades','Scholarship renewal for CHED Tulong Dunong',1,'released','QR-COG-2025-22415C','2025-08-18 14:10:00',NULL,NULL,NULL),(4,11,'Certificate of Enrollment','SSS Educational Benefit Dependent Verification',1,'pending','QR-COE-2025-33901D','2025-08-28 08:45:00',NULL,NULL,NULL),(5,6,'Diploma (Duplicate Copy)','Lost original diploma during typhoon relocation',1,'pending','QR-DIP-2025-44119E','2025-08-29 15:20:00',NULL,NULL,NULL);
/*!40000 ALTER TABLE `transcript_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transferee_credited_subjects`
--

DROP TABLE IF EXISTS `transferee_credited_subjects`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `transferee_credited_subjects` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id` int(11) NOT NULL,
  `prev_school` varchar(150) NOT NULL,
  `prev_subject_code` varchar(50) NOT NULL,
  `prev_subject_title` varchar(150) NOT NULL,
  `prev_units` decimal(3,1) NOT NULL,
  `prev_grade` varchar(20) NOT NULL,
  `credited_to_subject_id` int(11) NOT NULL,
  `status` enum('pending','approved','rejected') NOT NULL DEFAULT 'approved',
  `evaluated_by` varchar(100) NOT NULL DEFAULT 'Registrar Evaluator',
  `evaluated_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `student_id` (`student_id`),
  KEY `credited_to_subject_id` (`credited_to_subject_id`),
  CONSTRAINT `transferee_credited_subjects_ibfk_1` FOREIGN KEY (`student_id`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `transferee_credited_subjects_ibfk_2` FOREIGN KEY (`credited_to_subject_id`) REFERENCES `subjects` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transferee_credited_subjects`
--

LOCK TABLES `transferee_credited_subjects` WRITE;
/*!40000 ALTER TABLE `transferee_credited_subjects` DISABLE KEYS */;
INSERT INTO `transferee_credited_subjects` VALUES (1,15,'Far Eastern University - Tech','CS101','Computer Concepts and Logic Formulation',3.0,'1.50',1,'approved','Dr. Rosalinda Santos','2024-08-10 11:20:00'),(2,15,'Far Eastern University - Tech','ENG101','College English & Communication Arts',3.0,'1.75',9,'approved','Dr. Rosalinda Santos','2024-08-10 11:25:00');
/*!40000 ALTER TABLE `transferee_credited_subjects` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `student_id_number` varchar(30) DEFAULT NULL,
  `name` varchar(150) NOT NULL,
  `email` varchar(150) NOT NULL,
  `password` varchar(255) NOT NULL,
  `role` enum('admin','registrar','student','faculty','guidance') NOT NULL DEFAULT 'student',
  `status` enum('active','inactive') NOT NULL DEFAULT 'active',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`),
  UNIQUE KEY `student_id_number` (`student_id_number`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user`
--

LOCK TABLES `user` WRITE;
/*!40000 ALTER TABLE `user` DISABLE KEYS */;
INSERT INTO `user` VALUES (1,'REG-2020-001','Dr. Rosalinda Santos','registrar@msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','registrar','active','2026-09-22 02:35:08'),(2,'FAC-2018-045','Engr. Danilo Castillo','danilo.castillo@msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','faculty','active','2026-09-22 02:35:08'),(3,'FAC-2019-012','Prof. Maria Victoria Cruz','maria.cruz@msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','faculty','active','2026-09-22 02:35:08'),(4,'GUI-2021-008','Ma. Lourdes Ramos, RGC','guidance@msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','guidance','active','2026-09-22 02:35:08'),(5,'2022-00101','Alyssa Bea C. Mendoza','alyssa.mendoza@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(6,'2022-00102','Joshua Ryan T. Fernandez','joshua.fernandez@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(7,'2023-00201','Kirsten Nicole G. Reyes','kirsten.reyes@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(8,'2023-00202','Carl Christian B. Del Rosario','carl.delrosario@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(9,'2024-00301','Samantha Chloe V. Alcantara','samantha.alcantara@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(10,'2024-00302','Angelo Miguel S. Bautista','angelo.bautista@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(11,'2025-00401','Patricia Mae D. Gutierrez','patricia.gutierrez@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(12,'2025-00402','John Gabriel E. Aquino','john.aquino@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(13,'2022-00103','Rochelle Ann P. Soriano','rochelle.soriano@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(14,'2022-00104','Mark Dave L. Villanueva','mark.villanueva@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(15,'2023-00205','Christian Paul Z. Tan','christian.tan@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08'),(16,'2024-00308','Jasmine Joyce R. Navarro','jasmine.navarro@student.msu.edu.ph','$2y$10$iz4vZpvnDKRp6vPtlV5PZOPQANCi2DcbTJKUIZlYPQ48WWD107kYG','student','active','2026-09-22 02:35:08');
/*!40000 ALTER TABLE `user` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-09-22  2:35:15

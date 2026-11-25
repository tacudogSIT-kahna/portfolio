-- seed.sql : sample data for the Portfolio database
-- Run this after schema.sql. Courses and projects are numbered 1, 2, 3 ... in the order below.

INSERT INTO portfolio.course (code, title, units, term_taken, description) VALUES
  ('DMBWEB1', 'Database Management and Web Systems', 4, '1st Sem SY 2025-2026', 'Backend development with PostgreSQL and ASP.NET Core MVC.');

INSERT INTO portfolio.project (course_id, title, summary, video_url, completed_on) VALUES
  (1, 'Library', 'Creating a simple library management system.', NULL, '2026-11-16');

INSERT INTO portfolio.project_image (project_id, image_url) VALUES
  (1, 'https://placehold.co/800x450.png?text=Library');

SELECT (SELECT COUNT(*) FROM portfolio.course)        AS courses,
       (SELECT COUNT(*) FROM portfolio.project)       AS projects,
       (SELECT COUNT(*) FROM portfolio.project_image) AS images;
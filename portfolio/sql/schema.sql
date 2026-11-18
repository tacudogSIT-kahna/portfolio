-- schema.sql : the Portfolio database (schema "portfolio")

DROP SCHEMA IF EXISTS portfolio CASCADE;
CREATE SCHEMA portfolio;

-- One row per course I have taken.
CREATE TABLE portfolio.course (
    course_id   BIGINT        GENERATED ALWAYS AS IDENTITY,
    code        VARCHAR(15)   NOT NULL,
    title       VARCHAR(100)  NOT NULL,
    units       INTEGER       NOT NULL DEFAULT 3,
    term_taken  VARCHAR(30)   NOT NULL,
    description TEXT,
    CONSTRAINT pk_course        PRIMARY KEY (course_id),
    CONSTRAINT uq_course_code   UNIQUE (code),
    CONSTRAINT chk_course_units CHECK (units BETWEEN 1 AND 6)
);

-- ON DELETE RESTRICT: a course that still has projects cannot be deleted (SQLSTATE 23503).
CREATE TABLE portfolio.project (
    project_id   BIGINT        GENERATED ALWAYS AS IDENTITY,
    course_id    BIGINT        NOT NULL,
    title        VARCHAR(100)  NOT NULL,
    summary      TEXT,
    video_url    VARCHAR(200),
    completed_on DATE          NOT NULL DEFAULT CURRENT_DATE,
    CONSTRAINT pk_project            PRIMARY KEY (project_id),
    CONSTRAINT fk_project_course     FOREIGN KEY (course_id)
                                     REFERENCES portfolio.course (course_id) ON DELETE RESTRICT,
    CONSTRAINT chk_project_completed CHECK (completed_on <= CURRENT_DATE)
);

-- One row per image of a project. A project has many images.
-- ON DELETE CASCADE: deleting a project deletes its images with it.
-- UNIQUE (project_id, image_url): the same URL cannot be added twice to one project (SQLSTATE 23505).
CREATE TABLE portfolio.project_image (
    image_id   BIGINT        GENERATED ALWAYS AS IDENTITY,
    project_id BIGINT        NOT NULL,
    image_url  VARCHAR(500)  NOT NULL,
    CONSTRAINT pk_project_image     PRIMARY KEY (image_id),
    CONSTRAINT fk_image_project     FOREIGN KEY (project_id)
                                    REFERENCES portfolio.project (project_id) ON DELETE CASCADE,
    CONSTRAINT uq_image_project_url UNIQUE (project_id, image_url),
    CONSTRAINT chk_image_url        CHECK (image_url ~* '^https?://')
);

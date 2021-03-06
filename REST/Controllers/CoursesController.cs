﻿using REST.Models;
using REST.Models.Entities;
using REST.Representations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace REST.Controllers
{
    public class CoursesController : ApiController
    {
        private UniversityModel db = new UniversityModel();

        public CourseListRepresentation GetCourses()
        {
            List<Course> courseList = db.Courses.ToList();


            return new CourseListRepresentation( courseList.Select( (Course course) => ToCourseRepresentation(ToPOCO(course)) ).ToList() );
        }

        public CourseRepresentation GetCourse(int id)
        {
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return ToCourseRepresentation(ToPOCO(course));
        }

        [ResponseType(typeof(void))]
        public IHttpActionResult PutCourse(int id, Course course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (course == null)
            {
                return BadRequest();
            }

            UpdateCourse(course, id);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [ResponseType(typeof(void))]
        public IHttpActionResult PostCourse(Course course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((course == null) || !TeachersExist(course))
            {
                return BadRequest();
            }

            AttachTeachers(course);

            try
            {
                db.Courses.Add(course);
                db.SaveChanges();
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is DbUpdateException)
            {
                return BadRequest();
            }

            return StatusCode(HttpStatusCode.Created);
        }

        [ResponseType(typeof(void))]
        public IHttpActionResult DeleteCourse(int id)
        {
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }

            db.Courses.Remove(course);
            db.SaveChanges();

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CourseExists(int id)
        {
            return db.Courses.Count(e => e.Id == id) > 0;
        }

        private bool TeachersExist(Course course)
        {
            if (course.Teachers == null)
            {
                return true;
            }

            foreach (Teacher teacher in course.Teachers)
            {
                if (db.Teachers.Find(teacher.Id) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void AttachTeachers(Course course)
        {
            if (course.Teachers == null)
            {
                return;
            }

            List<Teacher> teachers = new List<Teacher>();
            foreach (Teacher teacher in course.Teachers)
            {
                teachers.Add(db.Teachers.Find(teacher.Id));
            }

            course.Teachers = teachers;
        }

        private void UpdateTeachers(Course updatingCourse, Course course)
        {
            if (course.Teachers == null)
            { 
                return;
            }

            List<Teacher> teachers = new List<Teacher>();
            course.Teachers.ForEach( (Teacher t) => teachers.Add(db.Teachers.Find(t.Id)) );

            updatingCourse.Teachers.Clear();
            foreach (Teacher teacher in teachers)
            {
                updatingCourse.Teachers.Add(teacher);
            }
        }

        private void UpdateCourse(Course course, int id)
        {
            Course updatingCourse = db.Courses.Include("Teachers").Single((Course c) => c.Id == id);
            UpdateTeachers(updatingCourse, course);
            if (course.Name != null)
            {
                updatingCourse.Name = course.Name;
            }
        }

        private Course ToPOCO(Course course)
        {
            List<Teacher> teachersList = null;
            if (course.Teachers != null)
            {
                teachersList = course.Teachers.Select((Teacher teacher) => new Teacher()
                {
                    Id = teacher.Id,
                    FullName = teacher.FullName,
                    ChairId = teacher.ChairId,
                    Chair = null,
                    Courses = null
                }).ToList();
            }

            return new Course()
            {
                Id = course.Id,
                Name = course.Name,
                Teachers = teachersList
            };
        }

        private CourseRepresentation ToCourseRepresentation(Course course)
        {
            List<int> teachersIds = null;
            if (course.Teachers != null)
            {
                teachersIds = course.Teachers.Select((Teacher t) => t.Id).ToList();
            }

            return new CourseRepresentation()
            {
                Id = course.Id,
                Name = course.Name,
                TeachersIds = teachersIds
            };
        }
    }
}
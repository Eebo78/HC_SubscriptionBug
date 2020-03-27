using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
namespace Hc
{
    public class Query
    {
        /// <summary>
        /// Gets all students.
        /// </summary>
        [UseSelection]
        public IQueryable<Student> GetStudents([Service]Context context) =>
           context.Students;
    }

    public class Mutation
    {
        public async Task<bool> AddWhatEver([Service]Context context, [Service]ITopicEventSender topicEventSender)
        {
            var course = context.Courses.First();

            var faker = new Faker();

            var newStudent = new Student { FirstMidName = faker.Name.FirstName(), LastName = faker.Name.LastName(), EnrollmentDate = DateTime.UtcNow };
            context.Enrollments.Add(new Enrollment
            {
                Course = course,
                Student = newStudent
            });

            await context.SaveChangesAsync();

            await topicEventSender.SendAsync("newStudent", newStudent.Id).ConfigureAwait(false);

            return true;
        }
    }



    public class Subscriptions
    {
        [Subscribe(nameof(OnNewStudent))]
        public async ValueTask<IAsyncEnumerable<int>> OnNewStudentChangedSubscription(
           [Service]ITopicEventReceiver eventTopicObserver,
           CancellationToken cancellationToken)
        {
            return await eventTopicObserver.SubscribeAsync<string, int>(
                "newStudent", cancellationToken).ConfigureAwait(false);
        }

        [UseSelection]
        // Using this throws: "extensions": {
        //     "message": "Value cannot be null. (Parameter 'source')",
        //     "stackTrace": "   at System.Linq.Queryable.Select[TSource,TResult](IQueryable`1 source, Expression`1 selector)\r\n   at HotChocolate.Types.Selections.SelectionMiddleware`1.InvokeAsync(IMiddlewareContext context)\r\n   at HotChocolate.Execution.ExecutionStrategyBase.ExecuteMiddlewareAsync(ResolverContext resolverContext, IErrorHandler errorHandler)"
        //   }
        // Reomving it I can query for the studen properties, but not get courses
        public async Task<Student> OnNewStudent([EventMessage]int studentId,
           [Service]Context context)
        {
            return await context.Students.FindAsync(studentId);
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace HayChoriYPaty.Tests
{
    public sealed class FirebasePlayerSessionDiagnosticTests
    {
        [Test]
        public void FailureDiagnosticKeepsStageAndFirebaseCodeButNeverExceptionMessage()
        {
            Type firebaseExceptionType = Type.GetType("Firebase.FirebaseException, Firebase.App", true);
            ConstructorInfo firebaseConstructor = firebaseExceptionType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(constructor =>
                {
                    ParameterInfo[] parameters = constructor.GetParameters();
                    return parameters.Length == 2 && parameters.Any(parameter => parameter.ParameterType == typeof(int)) &&
                        parameters.Any(parameter => parameter.ParameterType == typeof(string));
                });
            Assert.IsNotNull(firebaseConstructor, "The Firebase SDK exception constructor should expose an integer code and message.");

            const int expectedCode = 17;
            const string sensitiveMessage = "uid=private-user email=player@example.test idToken=private-token";
            object[] constructorArguments = firebaseConstructor.GetParameters()
                .Select(parameter => parameter.ParameterType == typeof(int) ? (object)expectedCode : sensitiveMessage)
                .ToArray();
            var firebaseError = (Exception)firebaseConstructor.Invoke(constructorArguments);
            var wrapped = new InvalidOperationException("outer " + sensitiveMessage, firebaseError);

            Type sessionType = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            MethodInfo formatter = sessionType.GetMethod("FormatFailureDiagnostic", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(formatter);

            string diagnostic = (string)formatter.Invoke(null, new object[] { "firestore-read", wrapped });

            StringAssert.Contains("stage=firestore-read", diagnostic);
            StringAssert.Contains("exceptionType=InvalidOperationException", diagnostic);
            StringAssert.Contains("firebaseErrorCode=" + expectedCode, diagnostic);
            StringAssert.DoesNotContain(sensitiveMessage, diagnostic);
            StringAssert.DoesNotContain("private-user", diagnostic);
            StringAssert.DoesNotContain("player@example.test", diagnostic);
            StringAssert.DoesNotContain("private-token", diagnostic);
        }

        [Test]
        public void FailureDiagnosticIncludesFirestoreCodeWithoutExceptionMessage()
        {
            const string sensitiveMessage = "uid=private-user email=player@example.test idToken=private-token";
            Exception firestoreError = CreateFirestoreException("PermissionDenied", sensitiveMessage);
            var wrapped = new InvalidOperationException("outer " + sensitiveMessage, firestoreError);

            Type sessionType = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            MethodInfo formatter = sessionType.GetMethod("FormatFailureDiagnostic", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(formatter);

            string diagnostic = (string)formatter.Invoke(null, new object[] { "firestore-commit", wrapped });

            StringAssert.Contains("stage=firestore-commit", diagnostic);
            StringAssert.Contains("exceptionType=InvalidOperationException", diagnostic);
            StringAssert.Contains("firestoreErrorCode=PermissionDenied", diagnostic);
            StringAssert.DoesNotContain(sensitiveMessage, diagnostic);
            StringAssert.DoesNotContain("private-user", diagnostic);
            StringAssert.DoesNotContain("player@example.test", diagnostic);
            StringAssert.DoesNotContain("private-token", diagnostic);
        }

        [TestCase("Unavailable", true)]
        [TestCase("DeadlineExceeded", true)]
        [TestCase("Cancelled", true)]
        [TestCase("Aborted", true)]
        [TestCase("PermissionDenied", false)]
        public void FirestoreErrorsUseTransientRetryPolicy(string code, bool expectedTransient)
        {
            Exception firestoreError = CreateFirestoreException(code);
            Type sessionType = Type.GetType("HayChoriYPaty.Cloud.FirebasePlayerSession, Assembly-CSharp", true);
            MethodInfo classifier = sessionType.GetMethod("IsTransient", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Exception) }, null);
            Assert.IsNotNull(classifier);

            bool actualTransient = (bool)classifier.Invoke(null, new object[] { firestoreError });

            Assert.AreEqual(expectedTransient, actualTransient);
        }

        private static Exception CreateFirestoreException(string codeName, string message = null)
        {
            Type exceptionType = Type.GetType("Firebase.Firestore.FirestoreException, Firebase.Firestore", true);
            Type codeType = exceptionType.GetProperty("ErrorCode").PropertyType;
            object code = Enum.Parse(codeType, codeName);
            Type[] signature = message == null ? new[] { codeType } : new[] { codeType, typeof(string) };
            ConstructorInfo constructor = exceptionType.GetConstructor(signature);
            Assert.IsNotNull(constructor, "The Firestore SDK should expose its documented exception constructor.");
            object[] arguments = message == null ? new[] { code } : new[] { code, message };
            return (Exception)constructor.Invoke(arguments);
        }
    }
}

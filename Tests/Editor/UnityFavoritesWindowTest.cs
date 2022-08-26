using System;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class UnityFavoritesWindowTest
    {
        [UnityTest]
        public IEnumerator TestMethod_TestingHow_TestResult()
        {
            // Arrange

            // Act
            yield return null;

            // Assert
            Assert.AreEqual(1, 1);
        }
    }

    public class SetUpAttribute : Attribute
    {
    }
}
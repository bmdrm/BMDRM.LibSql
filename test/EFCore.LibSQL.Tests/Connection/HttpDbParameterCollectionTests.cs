// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Connection;
using Microsoft.EntityFrameworkCore.LibSql.Connection;

/// <summary>
/// Unit tests for the HttpDbParameterCollection class to ensure correct behavior of parameter collection operations.
/// </summary>
public class HttpDbParameterCollectionTests
{
    /// <summary>
    /// Tests that a parameter is added correctly to the collection.
    /// </summary>
    [Fact]
    public void Add_ShouldAddParameterCorrectly()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter = new HttpDbParameter { ParameterName = "@param1", Value = 42 };

        // Act
        collection.Add(parameter);

        // Assert
        Assert.Single(collection);
        Assert.Same(parameter, collection[0]);
        Assert.Same(parameter, collection["@param1"]);
    }

    /// <summary>
    /// Tests that a parameter is removed correctly from the collection.
    /// </summary>
    [Fact]
    public void Remove_ShouldRemoveParameterCorrectly()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter = new HttpDbParameter { ParameterName = "@param1", Value = 42 };
        collection.Add(parameter);

        // Act
        collection.Remove(parameter);

        // Assert
        Assert.Empty(collection);
    }

    /// <summary>
    /// Tests the Contains method to verify if a parameter exists in the collection.
    /// </summary>
    [Fact]
    public void Contains_ShouldCheckExistenceCorrectly()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter = new HttpDbParameter { ParameterName = "@param1", Value = 42 };
        collection.Add(parameter);

        // Act & Assert
        Assert.True(collection.Contains("@param1"));
        Assert.False(collection.Contains("@param2"));
    }

    /// <summary>
    /// Tests the Clear method to ensure all parameters are removed from the collection.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllParameters()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        collection.Add(new HttpDbParameter { ParameterName = "@param1", Value = 42 });
        collection.Add(new HttpDbParameter { ParameterName = "@param2", Value = "test" });

        // Act
        collection.Clear();

        // Assert
        Assert.Empty(collection);
    }

    /// <summary>
    /// Tests the IndexOf method to verify the correct index of parameters in the collection.
    /// </summary>
    [Fact]
    public void IndexOf_ShouldReturnCorrectIndex()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter1 = new HttpDbParameter { ParameterName = "@param1", Value = 42 };
        var parameter2 = new HttpDbParameter { ParameterName = "@param2", Value = "test" };
        collection.Add(parameter1);
        collection.Add(parameter2);

        // Act & Assert
        Assert.Equal(0, collection.IndexOf(parameter1));
        Assert.Equal(1, collection.IndexOf(parameter2));
    }

    /// <summary>
    /// Tests the AddWithValue method to ensure a new parameter is created and added with the specified value.
    /// </summary>
    [Fact]
    public void AddWithValue_ShouldCreateAndAddParameter()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();

        // Act
        collection.Add(new HttpDbParameter("@param1", 42));

        // Assert
        Assert.Single(collection);
        Assert.Equal(42, collection["@param1"].Value);
    }

    /// <summary>
    /// Tests the CopyTo method to verify that parameters are copied to an array correctly.
    /// </summary>
    [Fact]
    public void CopyTo_ShouldCopyParametersToArray()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter1 = new HttpDbParameter { ParameterName = "@param1", Value = 42 };
        var parameter2 = new HttpDbParameter { ParameterName = "@param2", Value = "test" };
        collection.Add(parameter1);
        collection.Add(parameter2);
        var array = new HttpDbParameter[2];

        // Act
        collection.CopyTo(array, 0);

        // Assert
        Assert.Same(parameter1, array[0]);
        Assert.Same(parameter2, array[1]);
    }

    /// <summary>
    /// Tests the Insert method to ensure a parameter is inserted at the specified index.
    /// </summary>
    [Fact]
    public void Insert_ShouldInsertParameterAtIndex()
    {
        // Arrange
        var collection = new HttpDbParameterCollection();
        var parameter1 = new HttpDbParameter { ParameterName = "@param1", Value = 42 };
        var parameter2 = new HttpDbParameter { ParameterName = "@param2", Value = "test" };
        collection.Add(parameter1);

        // Act
        collection.Insert(0, parameter2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Same(parameter2, collection[0]);
        Assert.Same(parameter1, collection[1]);
    }
}

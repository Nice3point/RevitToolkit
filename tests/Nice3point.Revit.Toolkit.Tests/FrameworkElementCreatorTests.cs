using System.Windows.Controls;
using Nice3point.Revit.Toolkit.Options;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class FrameworkElementCreatorTests : RevitApiUiTest
{
    [Test]
    public async Task CreateFrameworkElement_DefaultConstructor_CreatesTheElementType()
    {
        // Arrange
        var creator = new FrameworkElementCreator<Border>();

        // Act
        var element = creator.CreateFrameworkElement();

        // Assert
        await Assert.That(element).IsTypeOf<Border>();
    }

    [Test]
    public async Task CreateFrameworkElement_DefaultConstructor_CreatesANewElementPerCall()
    {
        // Arrange
        var creator = new FrameworkElementCreator<Border>();

        // Act
        var firstElement = creator.CreateFrameworkElement();
        var secondElement = creator.CreateFrameworkElement();

        // Assert
        await Assert.That(firstElement).IsNotSameReferenceAs(secondElement);
    }

    [Test]
    public async Task CreateFrameworkElement_ServiceProvider_ReturnsTheRegisteredElement()
    {
        // Arrange
        var registeredElement = new Border();
        var creator = new FrameworkElementCreator<Border>(new ElementServiceProvider(registeredElement));

        // Act
        var element = creator.CreateFrameworkElement();

        // Assert
        await Assert.That(element).IsSameReferenceAs(registeredElement);
    }

    [Test]
    public async Task CreateFrameworkElement_ServiceProviderWithoutTheElement_ReturnsNull()
    {
        // Arrange
        var creator = new FrameworkElementCreator<Border>(new ElementServiceProvider(null));

        // Act
        var element = creator.CreateFrameworkElement();

        // Assert
        await Assert.That(element).IsNull();
    }

    private sealed class ElementServiceProvider(Border? element) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(Border) ? element : null;
        }
    }
}

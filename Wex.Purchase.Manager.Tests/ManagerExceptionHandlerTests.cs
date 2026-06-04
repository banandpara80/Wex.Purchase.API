using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using Wex.Purchase.Manager;
using Wex.Purchase.Manager.Exceptions;
using Wex.Purchase.BusinessModels;

namespace Wex.Purchase.Manager.Tests
{
    public class ManagerExceptionHandlerTests
    {
        [Fact]
        public async Task PurchaseManagerDecorator_Invokes_Handler_On_Exception()
        {
            var realMock = new Mock<PurchaseManager>(/* constructor args if any */) { CallBase = false };
            realMock.Setup(m => m.CreateAsync(It.IsAny<PurchaseRequestDTO>())).ThrowsAsync(new InvalidOperationException());

            var handlerMock = new Mock<IManagerExceptionHandler>();

            var decorator = new PurchaseManagerDecorator(realMock.Object, handlerMock.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.CreateAsync(new PurchaseRequestDTO()));

            handlerMock.Verify(h => h.Handle(It.IsAny<Exception>()), Times.Once);
        }
    }
}

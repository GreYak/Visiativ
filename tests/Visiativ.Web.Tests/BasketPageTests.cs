using Bunit;
using NSubstitute;
using NUnit.Framework;
using System.Net;
using Visiativ.Web;
using Visiativ.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Visiativ.Web.Tests;

[TestFixture]
public class BasketPageTests
{
    private TestContext ctx = null!;
    private IVisiativApiClient api = null!;

    private static BasketItemResponse[] DeuxArticles() =>
    [
        new(Guid.NewGuid(), "Laptop Pro",  999.99m, 1),
        new(Guid.NewGuid(), "Souris USB",   29.99m, 2),
    ];

    [SetUp]
    public void SetUp()
    {
        ctx = new TestContext();
        api = Substitute.For<IVisiativApiClient>();
        ctx.Services.AddSingleton(api);
    }

    [TearDown]
    public void TearDown() => ctx.Dispose();

    [Test]
    public void AffichageChargement_AvantDonnes()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(_ =>
            Task.Delay(Timeout.Infinite).ContinueWith<BasketItemResponse[]>(_ => []));

        var cut = ctx.RenderComponent<Basket>();

        Assert.That(cut.Markup, Does.Contain("Chargement de votre panier"));
    }

    [Test]
    public void AfficheVideMessage_SiPanierVide()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(Array.Empty<BasketItemResponse>());

        var cut = ctx.RenderComponent<Basket>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("Votre panier est vide")));
    }

    [Test]
    public void AfficheLignes_SiPanierNonVide()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(DeuxArticles());

        var cut = ctx.RenderComponent<Basket>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));
    }

    [Test]
    public void BoutonVider_VidePanier_SiApiOk()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(DeuxArticles());
        api.ClearBasketAsync(Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        var cut = ctx.RenderComponent<Basket>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("Votre panier est vide")));
    }

    [Test]
    public void BoutonPayer_VidePanier_SiApiOk()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(DeuxArticles());
        api.ClearBasketAsync(Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        var cut = ctx.RenderComponent<Basket>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));

        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("Votre panier est vide")));
    }

    [Test]
    public void BoutonVider_AfficheErreur_SiApiEchoue()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(DeuxArticles());
        api.ClearBasketAsync(Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var cut = ctx.RenderComponent<Basket>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent,
                Does.Contain("Impossible de vider le panier")));
    }

    [Test]
    public void AfficheErreur_SiChargementEchoue()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs<BasketItemResponse[]>(_ =>
            throw new HttpRequestException("Service indisponible"));

        var cut = ctx.RenderComponent<Basket>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent,
                Does.Contain("Impossible de charger le panier")));
    }
}

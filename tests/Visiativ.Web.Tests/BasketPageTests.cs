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

    private static BasketResult DeuxArticles() => new(
        Items:
        [
            new(Guid.NewGuid(), "Laptop Pro", "Ordinateur haute gamme", 999.99m, Quantity: 1, Stock: 10),
            new(Guid.NewGuid(), "Souris USB", "Souris sans fil",         29.99m, Quantity: 2, Stock:  5),
        ],
        IsPartial: false);

    private static BasketResult PanierPartiel() => new(
        Items: [new(Guid.NewGuid(), "Laptop Pro", "Ordinateur haute gamme", 999.99m, Quantity: 1, Stock: 10)],
        IsPartial: true);

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
            Task.Delay(Timeout.Infinite).ContinueWith<BasketResult>(_ => new([], false)));

        var cut = ctx.RenderComponent<Basket>();

        Assert.That(cut.Markup, Does.Contain("Chargement de votre panier"));
    }

    [Test]
    public void AfficheVideMessage_SiPanierVide()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(new BasketResult([], false));

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
    public void AfficheAvertissement_SiPanierPartiel()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs(PanierPartiel());

        var cut = ctx.RenderComponent<Basket>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-warning").TextContent,
                Does.Contain("retirés du panier").IgnoreCase));
    }

    [Test]
    public void AfficheErreur_SiChargementEchoue()
    {
        api.GetBasketAsync(default).ReturnsForAnyArgs<BasketResult>(_ =>
            throw new HttpRequestException("Service indisponible"));

        var cut = ctx.RenderComponent<Basket>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent,
                Does.Contain("Impossible de charger le panier")));
    }
}

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
public class ProductsPageTests
{
    private TestContext ctx = null!;
    private IVisiativApiClient api = null!;

    private static readonly Guid ProductId = Guid.NewGuid();

    private static ProductResponse ProductAvecStock(int stock = 5) =>
        new(ProductId, "Laptop Pro", "Description", 999.99m, stock);

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
        // GetProductsAsync ne complète jamais (bloque)
        api.GetProductsAsync(default).ReturnsForAnyArgs(_ =>
            Task.Delay(Timeout.Infinite).ContinueWith<ProductResponse[]>(_ => []));

        var cut = ctx.RenderComponent<Products>();

        Assert.That(cut.Markup, Does.Contain("Chargement"));
    }

    [Test]
    public void AfficheProduits_ApresChargement()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs(
            [ProductAvecStock(), new(Guid.NewGuid(), "Souris", "Desc", 29.99m, 3)]);

        var cut = ctx.RenderComponent<Products>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(2)));
    }

    [Test]
    public void AfficheRupture_PourStockZero()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 0)]);

        var cut = ctx.RenderComponent<Products>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".badge.bg-danger").TextContent, Is.EqualTo("Rupture")));
    }

    [Test]
    public void BoutonIncrement_Desactive_SiStockZero()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 0)]);

        var cut = ctx.RenderComponent<Products>();

        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button[disabled]");
            Assert.That(btn, Is.Not.Null);
        });
    }

    [Test]
    public void BoutonIncrement_DecrementeStockEtAfficheQuantite()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 5)]);

        var cut = ctx.RenderComponent<Products>();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1)));

        // clic sur +
        cut.Find("button.btn-outline-primary").Click();

        // stock affiché passe à 4, bouton "Add 1" apparaît
        Assert.That(cut.Markup, Does.Contain("4"));
        Assert.That(cut.Markup, Does.Contain("Add 1"));
    }

    [Test]
    public void BoutonAdd_AppelleApiEtResetQuantite()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 5)]);
        api.AddItemAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        var cut = ctx.RenderComponent<Products>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1)));

        // incrémenter la quantité
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Add 1")));

        // cliquer sur Add 1
        cut.Find("button.btn-success").Click();

        // le bouton Add disparaît (quantité remise à 0)
        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("button.btn-success"), Has.Count.EqualTo(0)));

        api.Received(1).AddItemAsync(ProductId, 1, Arg.Any<CancellationToken>());
    }

    [Test]
    public void BoutonAdd_AfficheErreur_SiApiEchoue()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 5)]);
        api.AddItemAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.BadRequest)
           {
               Content = new StringContent("Stock insuffisant")
           });

        var cut = ctx.RenderComponent<Products>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1)));

        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Add 1")));

        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("Erreur")));
    }

    [Test]
    public void BoutonAdd_AfficheDepassementDuStock_Si409()
    {
        api.GetProductsAsync(default).ReturnsForAnyArgs([ProductAvecStock(stock: 5)]);
        api.AddItemAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
           .Returns(new HttpResponseMessage(HttpStatusCode.Conflict)
           {
               Content = new StringContent("{\"message\":\"Oversize the limit: final quantity (7) exceeds the maximum allowed (5).\"}")
           });

        var cut = ctx.RenderComponent<Products>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("tbody tr"), Has.Count.EqualTo(1)));

        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Add 1")));

        cut.Find("button.btn-success").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent,
                Does.Contain("dépassement du stock").IgnoreCase));
    }
}

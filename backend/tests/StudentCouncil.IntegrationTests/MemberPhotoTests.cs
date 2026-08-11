using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace StudentCouncil.IntegrationTests;

// Fresh factory per test so each fact re-seeds the admin (its password change is isolated).
public class MemberPhotoTests : IAsyncLifetime
{
    private readonly RecordingEmailFactory _factory = new();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_factory).DisposeAsync();

    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private static MultipartFormDataContent PngUpload()
    {
        var content = new ByteArrayContent(PngBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent();
        form.Add(content, "file", "me.png");
        return form;
    }

    [Fact]
    public async Task Upload_avatar_then_stream_it_back()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        var put = await client.PutAsync("/api/v1/members/me/photo", PngUpload());
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var member = (await put.Content.ReadFromJsonAsync<MemberResponse>())!;
        member.PhotoUrl.Should().Be($"/api/v1/members/{member.Id}/photo");

        var get = await client.GetAsync(member.PhotoUrl);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        get.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await get.Content.ReadAsByteArrayAsync()).Take(8).Should().Equal(PngBytes.Take(8));
    }

    [Fact]
    public async Task Avatar_upload_rejects_non_image_formats()
    {
        var client = _factory.CreateClient();
        await Api.AuthenticateAdminAsync(client);

        var put = await client.PutAsync("/api/v1/members/me/photo", TaskApi.PdfUpload());

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Api.ReadCodeAsync(put)).Should().Be("unsupported_file_type");
    }
}

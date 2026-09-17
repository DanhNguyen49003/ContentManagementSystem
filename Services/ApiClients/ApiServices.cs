using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContentManagementSystem.ApplicationCore.DTOs;
using ContentManagementSystem.Service.Interface;

namespace ContentManagementSystem.Services.ApiClients
{
    // 1. PostApiService
    public class PostApiService : IPostService
    {
        private readonly IApiClient _api;
        public PostApiService(IApiClient api) => _api = api;

        public async Task<List<PostDto>> GetAllAsync()
            => await _api.GetAsync<List<PostDto>>("api/posts") ?? new List<PostDto>();

        public async Task<List<PostDto>> GetByAuthorIdAsync(string authorId)
            => await _api.GetAsync<List<PostDto>>($"api/posts?authorId={authorId}") ?? new List<PostDto>();

        public async Task<PostDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<PostDto>($"api/posts/{id}");

        public async Task<bool> CreateAsync(PostDto dto)
            => await _api.PostAsync("api/posts", dto);

        public async Task<bool> UpdateAsync(PostDto dto)
            => await _api.PutAsync($"api/posts/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/posts/{id}");

        public async Task<bool> TogglePublishAsync(Guid id)
            => await _api.PatchAsync($"api/posts/{id}/toggle-publish");

        public async Task<bool> ApproveAsync(Guid id, string reviewerId)
            => await _api.PatchAsync($"api/posts/{id}/approve");

        public async Task<bool> RejectAsync(Guid id, string reviewerId, string feedback)
            => await _api.PatchAsync($"api/posts/{id}/reject");

        public async Task<bool> RequestChangesAsync(Guid id, string reviewerId, string feedback)
            => await _api.PatchAsync($"api/posts/{id}/request-changes");

        public bool Exists(Guid id)
            => GetByIdAsync(id).GetAwaiter().GetResult() != null;
    }

    // 2. CategoryApiService
    public class CategoryApiService : ICategoryService
    {
        private readonly IApiClient _api;
        public CategoryApiService(IApiClient api) => _api = api;

        public async Task<List<CategoryDto>> GetAllAsync()
            => await _api.GetAsync<List<CategoryDto>>("api/categories") ?? new List<CategoryDto>();

        public async Task<CategoryDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<CategoryDto>($"api/categories/{id}");

        public async Task<bool> CreateAsync(CategoryDto dto)
            => await _api.PostAsync("api/categories", dto);

        public async Task<bool> UpdateAsync(CategoryDto dto)
            => await _api.PutAsync($"api/categories/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/categories/{id}");
    }

    // 3. TagApiService
    public class TagApiService : ITagService
    {
        private readonly IApiClient _api;
        public TagApiService(IApiClient api) => _api = api;

        public async Task<List<TagDto>> GetAllAsync()
            => await _api.GetAsync<List<TagDto>>("api/tags") ?? new List<TagDto>();

        public async Task<TagDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<TagDto>($"api/tags/{id}");

        public async Task<bool> CreateAsync(TagDto dto)
            => await _api.PostAsync("api/tags", dto);

        public async Task<bool> UpdateAsync(TagDto dto)
            => await _api.PutAsync($"api/tags/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/tags/{id}");
    }

    // 4. CommentApiService
    public class CommentApiService : ICommentService
    {
        private readonly IApiClient _api;
        public CommentApiService(IApiClient api) => _api = api;

        public async Task<List<CommentDto>> GetAllAsync()
            => await _api.GetAsync<List<CommentDto>>("api/comments") ?? new List<CommentDto>();

        public async Task<CommentDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<CommentDto>($"api/comments/{id}");

        public async Task<bool> CreateAsync(CommentDto dto)
            => await _api.PostAsync("api/comments", dto);

        public async Task<bool> UpdateAsync(CommentDto dto)
            => await _api.PutAsync($"api/comments/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/comments/{id}");

        public async Task<bool> ApproveAsync(Guid id)
            => await _api.PatchAsync($"api/comments/{id}/approve");

        public async Task<bool> RejectAsync(Guid id)
            => await _api.PatchAsync($"api/comments/{id}/reject");
    }

    // 5. PageApiService
    public class PageApiService : IPageService
    {
        private readonly IApiClient _api;
        public PageApiService(IApiClient api) => _api = api;

        public async Task<List<PageDto>> GetAllAsync()
            => await _api.GetAsync<List<PageDto>>("api/pages") ?? new List<PageDto>();

        public async Task<PageDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<PageDto>($"api/pages/{id}");

        public async Task<bool> CreateAsync(PageDto dto)
            => await _api.PostAsync("api/pages", dto);

        public async Task<bool> UpdateAsync(PageDto dto)
            => await _api.PutAsync($"api/pages/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/pages/{id}");
    }

    // 6. BannerApiService
    public class BannerApiService : IBannerService
    {
        private readonly IApiClient _api;
        public BannerApiService(IApiClient api) => _api = api;

        public async Task<List<BannerDto>> GetAllAsync()
            => await _api.GetAsync<List<BannerDto>>("api/banners") ?? new List<BannerDto>();

        public async Task<BannerDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<BannerDto>($"api/banners/{id}");

        public async Task<bool> CreateAsync(BannerDto dto)
            => await _api.PostAsync("api/banners", dto);

        public async Task<bool> UpdateAsync(BannerDto dto)
            => await _api.PutAsync($"api/banners/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/banners/{id}");
    }

    // 7. EventApiService
    public class EventApiService : IEventService
    {
        private readonly IApiClient _api;
        public EventApiService(IApiClient api) => _api = api;

        public async Task<List<EventDto>> GetAllAsync()
            => await _api.GetAsync<List<EventDto>>("api/events") ?? new List<EventDto>();

        public async Task<EventDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<EventDto>($"api/events/{id}");

        public async Task<bool> CreateAsync(EventDto dto)
            => await _api.PostAsync("api/events", dto);

        public async Task<bool> UpdateAsync(EventDto dto)
            => await _api.PutAsync($"api/events/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/events/{id}");
    }

    // 8. FaqApiService
    public class FaqApiService : IFaqService
    {
        private readonly IApiClient _api;
        public FaqApiService(IApiClient api) => _api = api;

        public async Task<List<FaqDto>> GetAllAsync()
            => await _api.GetAsync<List<FaqDto>>("api/faqs") ?? new List<FaqDto>();

        public async Task<FaqDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<FaqDto>($"api/faqs/{id}");

        public async Task<bool> CreateAsync(FaqDto dto)
            => await _api.PostAsync("api/faqs", dto);

        public async Task<bool> UpdateAsync(FaqDto dto)
            => await _api.PutAsync($"api/faqs/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/faqs/{id}");
    }

    // 9. PartnerApiService
    public class PartnerApiService : IPartnerService
    {
        private readonly IApiClient _api;
        public PartnerApiService(IApiClient api) => _api = api;

        public async Task<List<PartnerDto>> GetAllAsync()
            => await _api.GetAsync<List<PartnerDto>>("api/partners") ?? new List<PartnerDto>();

        public async Task<PartnerDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<PartnerDto>($"api/partners/{id}");

        public async Task<bool> CreateAsync(PartnerDto dto)
            => await _api.PostAsync("api/partners", dto);

        public async Task<bool> UpdateAsync(PartnerDto dto)
            => await _api.PutAsync($"api/partners/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/partners/{id}");
    }

    // 10. TestimonialApiService
    public class TestimonialApiService : ITestimonialService
    {
        private readonly IApiClient _api;
        public TestimonialApiService(IApiClient api) => _api = api;

        public async Task<List<TestimonialDto>> GetAllAsync()
            => await _api.GetAsync<List<TestimonialDto>>("api/testimonials") ?? new List<TestimonialDto>();

        public async Task<TestimonialDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<TestimonialDto>($"api/testimonials/{id}");

        public async Task<bool> CreateAsync(TestimonialDto dto)
            => await _api.PostAsync("api/testimonials", dto);

        public async Task<bool> UpdateAsync(TestimonialDto dto)
            => await _api.PutAsync($"api/testimonials/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/testimonials/{id}");
    }

    // 11. MenuApiService
    public class MenuApiService : IMenuService
    {
        private readonly IApiClient _api;
        public MenuApiService(IApiClient api) => _api = api;

        public async Task<List<MenuDto>> GetAllAsync()
            => await _api.GetAsync<List<MenuDto>>("api/menus") ?? new List<MenuDto>();

        public async Task<MenuDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<MenuDto>($"api/menus/{id}");

        public async Task<bool> CreateAsync(MenuDto dto)
            => await _api.PostAsync("api/menus", dto);

        public async Task<bool> UpdateAsync(MenuDto dto)
            => await _api.PutAsync($"api/menus/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/menus/{id}");
    }

    // 12. MenuItemApiService
    public class MenuItemApiService : IMenuItemService
    {
        private readonly IApiClient _api;
        public MenuItemApiService(IApiClient api) => _api = api;

        public async Task<List<MenuItemDto>> GetAllAsync()
            => await _api.GetAsync<List<MenuItemDto>>("api/menuitems") ?? new List<MenuItemDto>();

        public async Task<MenuItemDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<MenuItemDto>($"api/menuitems/{id}");

        public async Task<bool> CreateAsync(MenuItemDto dto)
            => await _api.PostAsync("api/menuitems", dto);

        public async Task<bool> UpdateAsync(MenuItemDto dto)
            => await _api.PutAsync($"api/menuitems/{dto.Id}", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/menuitems/{id}");
    }

    // 13. ContactMessageApiService
    public class ContactMessageApiService : IContactMessageService
    {
        private readonly IApiClient _api;
        public ContactMessageApiService(IApiClient api) => _api = api;

        public async Task<List<ContactMessageDto>> GetAllAsync()
            => await _api.GetAsync<List<ContactMessageDto>>("api/contactmessages") ?? new List<ContactMessageDto>();

        public async Task<ContactMessageDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<ContactMessageDto>($"api/contactmessages/{id}");

        public async Task<bool> CreateAsync(ContactMessageDto dto)
            => await _api.PostAsync("api/contactmessages", dto);

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/contactmessages/{id}");
    }

    // 14. NewsletterSubscriberApiService
    public class NewsletterSubscriberApiService : INewsletterSubscriberService
    {
        private readonly IApiClient _api;
        public NewsletterSubscriberApiService(IApiClient api) => _api = api;

        public async Task<List<NewsletterSubscriberDto>> GetAllAsync()
            => await _api.GetAsync<List<NewsletterSubscriberDto>>("api/newslettersubscribers") ?? new List<NewsletterSubscriberDto>();

        public async Task<NewsletterSubscriberDto?> GetByIdAsync(Guid id)
            => await _api.GetAsync<NewsletterSubscriberDto>($"api/newslettersubscribers/{id}");

        public async Task<bool> SubscribeAsync(string email)
            => await _api.PostAsync("api/newslettersubscribers/subscribe", new { Email = email });

        public async Task<bool> UnsubscribeAsync(Guid id)
            => await _api.DeleteAsync($"api/newslettersubscribers/{id}");

        public async Task<bool> DeleteAsync(Guid id)
            => await _api.DeleteAsync($"api/newslettersubscribers/{id}");
    }
}


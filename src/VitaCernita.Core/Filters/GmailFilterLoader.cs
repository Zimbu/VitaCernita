using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lua;
using Lua.Standard;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Configuration;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Validation;
using VitaCernita.Core.Queries;

namespace VitaCernita.Core.Filters;

public sealed class GmailFilterLoader
{
    private const string DslPrelude = @"
-- =======================================================================
-- VitaCernita DSL Prelude (Query, Action & Filter)
-- =======================================================================

function from(val) return { type = 'field', field = 'from', value = tostring(val) } end
From = from

function to(val) return { type = 'field', field = 'to', value = tostring(val) } end
To = to

function cc(val) return { type = 'field', field = 'cc', value = tostring(val) } end
Cc = cc

function bcc(val) return { type = 'field', field = 'bcc', value = tostring(val) } end
Bcc = bcc

function subject(val) return { type = 'field', field = 'subject', value = tostring(val) } end
Subject = subject

function list(val) return { type = 'field', field = 'list', value = tostring(val) } end
List = list

function filename(val) return { type = 'field', field = 'filename', value = tostring(val) } end
Filename = filename

function delivered_to(val) return { type = 'field', field = 'deliveredto', value = tostring(val) } end
deliveredto = delivered_to
DeliveredTo = delivered_to

function rfc822msgid(val) return { type = 'field', field = 'rfc822msgid', value = tostring(val) } end
Rfc822MsgId = rfc822msgid
msgid = rfc822msgid

function header(name_or_pair, maybe_val)
    if maybe_val ~= nil then
        return { type = 'field', field = 'header', value = tostring(name_or_pair) .. ':' .. tostring(maybe_val) }
    else
        return { type = 'field', field = 'header', value = tostring(name_or_pair) }
    end
end
Header = header

-- Exact word or phrase match: double-quoted search term
function match(phrase) return { type = 'exact', value = tostring(phrase) } end
Match = match
exact = match

-- Date operators
function after(val) return { type = 'field', field = 'after', value = tostring(val) } end
After = after

function before(val) return { type = 'field', field = 'before', value = tostring(val) } end
Before = before

function older(val) return { type = 'field', field = 'older', value = tostring(val) } end
Older = older

function newer(val) return { type = 'field', field = 'newer', value = tostring(val) } end
Newer = newer

-- Duration operators
function older_than(val) return { type = 'field', field = 'older_than', value = tostring(val) } end
OlderThan = older_than
olderThan = older_than

function newer_than(val) return { type = 'field', field = 'newer_than', value = tostring(val) } end
NewerThan = newer_than
newerThan = newer_than

-- Star and icon operators (callable or usable as constant value)
local function make_star(name)
    local tbl = { type = 'has', value = name }
    return setmetatable(tbl, {
        __call = function() return { type = 'has', value = name } end
    })
end

has_yellow_star = make_star('yellow-star')
HasYellowStar = has_yellow_star
has_orange_star = make_star('orange-star')
HasOrangeStar = has_orange_star
has_red_star = make_star('red-star')
HasRedStar = has_red_star
has_purple_star = make_star('purple-star')
HasPurpleStar = has_purple_star
has_blue_star = make_star('blue-star')
HasBlueStar = has_blue_star
has_green_star = make_star('green-star')
HasGreenStar = has_green_star

has_red_bang = make_star('red-bang')
HasRedBang = has_red_bang
has_yellow_bang = make_star('yellow-bang')
HasYellowBang = has_yellow_bang
has_orange_guillemet = make_star('orange-guillemet')
HasOrangeGuillemet = has_orange_guillemet
has_green_check = make_star('green-check')
HasGreenCheck = has_green_check
has_blue_info = make_star('blue-info')
HasBlueInfo = has_blue_info
has_purple_question = make_star('purple-question')
HasPurpleQuestion = has_purple_question

-- Generic has operator
function has(val) return { type = 'has', value = tostring(val) } end
Has = has

-- Media, Workspace documents, and label metadata has operators
has_attachment = make_star('attachment')
HasAttachment = has_attachment
attachment = has_attachment

has_drive = make_star('drive')
HasDrive = has_drive
drive = has_drive

has_document = make_star('document')
HasDocument = has_document
document = has_document

has_spreadsheet = make_star('spreadsheet')
HasSpreadsheet = has_spreadsheet
spreadsheet = has_spreadsheet

has_presentation = make_star('presentation')
HasPresentation = has_presentation
presentation = has_presentation

has_youtube = make_star('youtube')
HasYoutube = has_youtube
has_you_tube = has_youtube
youtube = has_youtube

has_user_labels = make_star('userlabels')
HasUserLabels = has_user_labels
user_labels = has_user_labels

has_no_user_labels = make_star('nouserlabels')
HasNoUserLabels = has_no_user_labels
no_user_labels = has_no_user_labels

-- Is operator & is_starred
local function make_is(name)
    local tbl = { type = 'is', value = name }
    return setmetatable(tbl, {
        __call = function() return { type = 'is', value = name } end
    })
end

is_starred = make_is('starred')
IsStarred = is_starred
starred = is_starred

is_unread = make_is('unread')
IsUnread = is_unread
unread = is_unread

is_read = make_is('read')
IsRead = is_read
read = is_read

is_important = make_is('important')
IsImportant = is_important
important = is_important

is_muted = make_is('muted')
IsMuted = is_muted
muted = is_muted

is_snoozed = make_is('snoozed')
IsSnoozed = is_snoozed
snoozed = is_snoozed

is_chat = make_is('chat')
IsChat = is_chat
chat = is_chat

is_draft = make_is('draft')
IsDraft = is_draft
draft = is_draft

is_sent = make_is('sent')
IsSent = is_sent

is_trash = make_is('trash')
IsTrash = is_trash

is_spam = make_is('spam')
IsSpam = is_spam

function is(val) return { type = 'is', value = tostring(val) } end
Is = is

-- In operator & location/folder helpers
local function make_in(name)
    local tbl = { type = 'in', value = name }
    return setmetatable(tbl, {
        __call = function() return { type = 'in', value = name } end
    })
end

function In(target) return { type = 'in', value = tostring(target) } end
in_folder = In
in_location = In
in_box = In

in_anywhere = make_in('anywhere')
InAnywhere = in_anywhere
anywhere = in_anywhere

in_archive = make_in('archive')
InArchive = in_archive
archive = setmetatable({ type = 'in', value = 'archive', action = 'archive' }, {
    __call = function() return { type = 'in', value = 'archive', action = 'archive' } end
})
Archive = archive

in_snoozed = make_in('snoozed')
InSnoozed = in_snoozed

in_inbox = make_in('inbox')
InInbox = in_inbox
inbox = in_inbox

in_sent = make_in('sent')
InSent = in_sent

in_drafts = make_in('drafts')
InDrafts = in_drafts
in_draft = in_drafts
drafts = in_drafts

in_trash = make_in('trash')
InTrash = in_trash
trash = in_trash

in_spam = make_in('spam')
InSpam = in_spam
spam = in_spam

in_chats = make_in('chats')
InChats = in_chats
in_chat = in_chats
chats = in_chats

-- Category operator & helpers
local function make_category(name)
    local tbl = { type = 'category', value = name }
    return setmetatable(tbl, {
        __call = function() return { type = 'category', value = name } end,
        __tostring = function() return name end
    })
end

function category(target) return { type = 'category', value = tostring(target) } end
Category = category

category_primary = make_category('primary')
CategoryPrimary = category_primary

category_social = make_category('social')
CategorySocial = category_social

category_promotions = make_category('promotions')
CategoryPromotions = category_promotions
category_promotion = category_promotions

category_updates = make_category('updates')
CategoryUpdates = category_updates
category_update = category_updates

category_forums = make_category('forums')
CategoryForums = category_forums
category_forum = category_forums

category_reservations = make_category('reservations')
CategoryReservations = category_reservations
category_reservation = category_reservations

category_purchases = make_category('purchases')
CategoryPurchases = category_purchases
category_purchase = category_purchases

-- Size operators & helpers
function size(val) return { type = 'size', op = 'size', value = tostring(val) } end
Size = size

function larger(val) return { type = 'size', op = 'larger', value = tostring(val) } end
Larger = larger
larger_than = larger
LargerThan = larger

function smaller(val) return { type = 'size', op = 'smaller', value = tostring(val) } end
Smaller = smaller
smaller_than = smaller
SmallerThan = smaller

-- Negation (NOT) operator
function Not(...)
    local args = { ... }
    if #args == 0 then
        return { type = 'operator', op = 'not' }
    elseif #args == 1 then
        return { type = 'operator', op = 'not', condition = args[1] }
    else
        return { type = 'operator', op = 'not', condition = And(...) }
    end
end
not_op = Not
negate = Not
invert = Not

-- Logical operators
function And(...)
    local args = { ... }
    return { type = 'operator', op = 'and', conditions = args }
end
all_of = And
All = And

function Or(...)
    local args = { ... }
    return { type = 'operator', op = 'or', conditions = args }
end
any_of = Or
Any = Or
either = Or

-- =======================================================================
-- Query Builder & query() DSL
-- =======================================================================
local QueryBuilder = {}
QueryBuilder.__index = QueryBuilder

function QueryBuilder:from(val) table.insert(self.conditions, from(val)); return self end
QueryBuilder.From = QueryBuilder.from
function QueryBuilder:to(val) table.insert(self.conditions, to(val)); return self end
QueryBuilder.To = QueryBuilder.to
function QueryBuilder:cc(val) table.insert(self.conditions, cc(val)); return self end
QueryBuilder.Cc = QueryBuilder.cc
function QueryBuilder:bcc(val) table.insert(self.conditions, bcc(val)); return self end
QueryBuilder.Bcc = QueryBuilder.bcc
function QueryBuilder:subject(val) table.insert(self.conditions, subject(val)); return self end
QueryBuilder.Subject = QueryBuilder.subject
function QueryBuilder:list(val) table.insert(self.conditions, list(val)); return self end
QueryBuilder.List = QueryBuilder.list
function QueryBuilder:filename(val) table.insert(self.conditions, filename(val)); return self end
QueryBuilder.Filename = QueryBuilder.filename
function QueryBuilder:delivered_to(val) table.insert(self.conditions, delivered_to(val)); return self end
QueryBuilder.DeliveredTo = QueryBuilder.delivered_to
function QueryBuilder:rfc822msgid(val) table.insert(self.conditions, rfc822msgid(val)); return self end
QueryBuilder.Rfc822MsgId = QueryBuilder.rfc822msgid
function QueryBuilder:header(name_or_pair, maybe_val) table.insert(self.conditions, header(name_or_pair, maybe_val)); return self end
QueryBuilder.Header = QueryBuilder.header
function QueryBuilder:label(val) table.insert(self.conditions, label(val)); return self end
QueryBuilder.Label = QueryBuilder.label
function QueryBuilder:match(phrase) table.insert(self.conditions, match(phrase)); return self end
QueryBuilder.Match = QueryBuilder.match
function QueryBuilder:after(val) table.insert(self.conditions, after(val)); return self end
QueryBuilder.After = QueryBuilder.after
function QueryBuilder:before(val) table.insert(self.conditions, before(val)); return self end
QueryBuilder.Before = QueryBuilder.before
function QueryBuilder:older(val) table.insert(self.conditions, older(val)); return self end
QueryBuilder.Older = QueryBuilder.older
function QueryBuilder:newer(val) table.insert(self.conditions, newer(val)); return self end
QueryBuilder.Newer = QueryBuilder.newer
function QueryBuilder:older_than(val) table.insert(self.conditions, older_than(val)); return self end
QueryBuilder.OlderThan = QueryBuilder.older_than
function QueryBuilder:newer_than(val) table.insert(self.conditions, newer_than(val)); return self end
QueryBuilder.NewerThan = QueryBuilder.newer_than
function QueryBuilder:has(val) table.insert(self.conditions, has(val)); return self end
QueryBuilder.Has = QueryBuilder.has
function QueryBuilder:has_yellow_star() table.insert(self.conditions, has_yellow_star()); return self end
QueryBuilder.HasYellowStar = QueryBuilder.has_yellow_star
function QueryBuilder:has_orange_star() table.insert(self.conditions, has_orange_star()); return self end
QueryBuilder.HasOrangeStar = QueryBuilder.has_orange_star
function QueryBuilder:has_red_star() table.insert(self.conditions, has_red_star()); return self end
QueryBuilder.HasRedStar = QueryBuilder.has_red_star
function QueryBuilder:has_purple_star() table.insert(self.conditions, has_purple_star()); return self end
QueryBuilder.HasPurpleStar = QueryBuilder.has_purple_star
function QueryBuilder:has_blue_star() table.insert(self.conditions, has_blue_star()); return self end
QueryBuilder.HasBlueStar = QueryBuilder.has_blue_star
function QueryBuilder:has_green_star() table.insert(self.conditions, has_green_star()); return self end
QueryBuilder.HasGreenStar = QueryBuilder.has_green_star
function QueryBuilder:has_red_bang() table.insert(self.conditions, has_red_bang()); return self end
QueryBuilder.HasRedBang = QueryBuilder.has_red_bang
function QueryBuilder:has_yellow_bang() table.insert(self.conditions, has_yellow_bang()); return self end
QueryBuilder.HasYellowBang = QueryBuilder.has_yellow_bang
function QueryBuilder:has_orange_guillemet() table.insert(self.conditions, has_orange_guillemet()); return self end
QueryBuilder.HasOrangeGuillemet = QueryBuilder.has_orange_guillemet
function QueryBuilder:has_green_check() table.insert(self.conditions, has_green_check()); return self end
QueryBuilder.HasGreenCheck = QueryBuilder.has_green_check
function QueryBuilder:has_blue_info() table.insert(self.conditions, has_blue_info()); return self end
QueryBuilder.HasBlueInfo = QueryBuilder.has_blue_info
function QueryBuilder:has_purple_question() table.insert(self.conditions, has_purple_question()); return self end
QueryBuilder.HasPurpleQuestion = QueryBuilder.has_purple_question
function QueryBuilder:has_attachment() table.insert(self.conditions, has_attachment()); return self end
QueryBuilder.HasAttachment = QueryBuilder.has_attachment
function QueryBuilder:has_drive() table.insert(self.conditions, has_drive()); return self end
QueryBuilder.HasDrive = QueryBuilder.has_drive
function QueryBuilder:has_document() table.insert(self.conditions, has_document()); return self end
QueryBuilder.HasDocument = QueryBuilder.has_document
function QueryBuilder:has_spreadsheet() table.insert(self.conditions, has_spreadsheet()); return self end
QueryBuilder.HasSpreadsheet = QueryBuilder.has_spreadsheet
function QueryBuilder:has_presentation() table.insert(self.conditions, has_presentation()); return self end
QueryBuilder.HasPresentation = QueryBuilder.has_presentation
function QueryBuilder:has_youtube() table.insert(self.conditions, has_youtube()); return self end
QueryBuilder.HasYoutube = QueryBuilder.has_youtube
function QueryBuilder:has_user_labels() table.insert(self.conditions, has_user_labels()); return self end
QueryBuilder.HasUserLabels = QueryBuilder.has_user_labels
function QueryBuilder:has_no_user_labels() table.insert(self.conditions, has_no_user_labels()); return self end
QueryBuilder.HasNoUserLabels = QueryBuilder.has_no_user_labels
function QueryBuilder:is(val) table.insert(self.conditions, is(val)); return self end
QueryBuilder.Is = QueryBuilder.is
function QueryBuilder:is_starred() table.insert(self.conditions, is_starred()); return self end
QueryBuilder.IsStarred = QueryBuilder.is_starred
QueryBuilder.starred = QueryBuilder.is_starred
function QueryBuilder:is_unread() table.insert(self.conditions, is_unread()); return self end
QueryBuilder.IsUnread = QueryBuilder.is_unread
QueryBuilder.unread = QueryBuilder.is_unread
function QueryBuilder:is_read() table.insert(self.conditions, is_read()); return self end
QueryBuilder.IsRead = QueryBuilder.is_read
QueryBuilder.read = QueryBuilder.is_read
function QueryBuilder:is_important() table.insert(self.conditions, is_important()); return self end
QueryBuilder.IsImportant = QueryBuilder.is_important
QueryBuilder.important = QueryBuilder.is_important
function QueryBuilder:is_muted() table.insert(self.conditions, is_muted()); return self end
QueryBuilder.IsMuted = QueryBuilder.is_muted
QueryBuilder.muted = QueryBuilder.is_muted
function QueryBuilder:is_snoozed() table.insert(self.conditions, is_snoozed()); return self end
QueryBuilder.IsSnoozed = QueryBuilder.is_snoozed
QueryBuilder.snoozed = QueryBuilder.is_snoozed
function QueryBuilder:is_chat() table.insert(self.conditions, is_chat()); return self end
QueryBuilder.IsChat = QueryBuilder.is_chat
function QueryBuilder:is_draft() table.insert(self.conditions, is_draft()); return self end
QueryBuilder.IsDraft = QueryBuilder.is_draft
function QueryBuilder:is_sent() table.insert(self.conditions, is_sent()); return self end
QueryBuilder.IsSent = QueryBuilder.is_sent
function QueryBuilder:is_trash() table.insert(self.conditions, is_trash()); return self end
QueryBuilder.IsTrash = QueryBuilder.is_trash
function QueryBuilder:is_spam() table.insert(self.conditions, is_spam()); return self end
QueryBuilder.IsSpam = QueryBuilder.is_spam
function QueryBuilder:In(target) table.insert(self.conditions, In(target)); return self end
QueryBuilder['in'] = QueryBuilder.In
QueryBuilder.in_folder = QueryBuilder.In
QueryBuilder.in_location = QueryBuilder.In
function QueryBuilder:in_anywhere() table.insert(self.conditions, in_anywhere()); return self end
QueryBuilder.InAnywhere = QueryBuilder.in_anywhere
function QueryBuilder:in_archive() table.insert(self.conditions, in_archive()); return self end
QueryBuilder.InArchive = QueryBuilder.in_archive
function QueryBuilder:in_snoozed() table.insert(self.conditions, in_snoozed()); return self end
QueryBuilder.InSnoozed = QueryBuilder.in_snoozed
function QueryBuilder:in_inbox() table.insert(self.conditions, in_inbox()); return self end
QueryBuilder.InInbox = QueryBuilder.in_inbox
function QueryBuilder:in_sent() table.insert(self.conditions, in_sent()); return self end
QueryBuilder.InSent = QueryBuilder.in_sent
function QueryBuilder:in_drafts() table.insert(self.conditions, in_drafts()); return self end
QueryBuilder.InDrafts = QueryBuilder.in_drafts
function QueryBuilder:in_trash() table.insert(self.conditions, in_trash()); return self end
QueryBuilder.InTrash = QueryBuilder.in_trash
function QueryBuilder:in_spam() table.insert(self.conditions, in_spam()); return self end
QueryBuilder.InSpam = QueryBuilder.in_spam
function QueryBuilder:in_chats() table.insert(self.conditions, in_chats()); return self end
QueryBuilder.InChats = QueryBuilder.in_chats
function QueryBuilder:category(target) table.insert(self.conditions, category(target)); return self end
QueryBuilder.Category = QueryBuilder.category
function QueryBuilder:category_primary() table.insert(self.conditions, category_primary()); return self end
QueryBuilder.CategoryPrimary = QueryBuilder.category_primary
function QueryBuilder:category_social() table.insert(self.conditions, category_social()); return self end
QueryBuilder.CategorySocial = QueryBuilder.category_social
function QueryBuilder:category_promotions() table.insert(self.conditions, category_promotions()); return self end
QueryBuilder.CategoryPromotions = QueryBuilder.category_promotions
function QueryBuilder:category_updates() table.insert(self.conditions, category_updates()); return self end
QueryBuilder.CategoryUpdates = QueryBuilder.category_updates
function QueryBuilder:category_forums() table.insert(self.conditions, category_forums()); return self end
QueryBuilder.CategoryForums = QueryBuilder.category_forums
function QueryBuilder:category_reservations() table.insert(self.conditions, category_reservations()); return self end
QueryBuilder.CategoryReservations = QueryBuilder.category_reservations
function QueryBuilder:category_purchases() table.insert(self.conditions, category_purchases()); return self end
QueryBuilder.CategoryPurchases = QueryBuilder.category_purchases
function QueryBuilder:size(val) table.insert(self.conditions, size(val)); return self end
QueryBuilder.Size = QueryBuilder.size
function QueryBuilder:larger(val) table.insert(self.conditions, larger(val)); return self end
QueryBuilder.Larger = QueryBuilder.larger
QueryBuilder.larger_than = QueryBuilder.larger
QueryBuilder.LargerThan = QueryBuilder.larger
function QueryBuilder:smaller(val) table.insert(self.conditions, smaller(val)); return self end
QueryBuilder.Smaller = QueryBuilder.smaller
QueryBuilder.smaller_than = QueryBuilder.smaller
QueryBuilder.SmallerThan = QueryBuilder.smaller
function QueryBuilder:Not(...) table.insert(self.conditions, Not(...)); return self end
QueryBuilder['not'] = QueryBuilder.Not
QueryBuilder.not_op = QueryBuilder.Not
QueryBuilder.negate = QueryBuilder.Not
QueryBuilder.invert = QueryBuilder.Not

function QueryBuilder:build()
    return { type = 'query', conditions = self.conditions }
end

function query(arg)
    if arg == nil then
        return setmetatable({ type = 'query_builder', conditions = {} }, QueryBuilder)
    elseif type(arg) == 'table' then
        return setmetatable({ type = 'query', definition = arg }, {
            __index = arg
        })
    else
        return { type = 'query', query = tostring(arg) }
    end
end
Query = query

-- =======================================================================
-- Action Builder & action() DSL
-- =======================================================================
local function make_action_item(action_name)
    local tbl = { type = 'action_item', action = action_name }
    return setmetatable(tbl, {
        __call = function() return { type = 'action_item', action = action_name } end
    })
end

mark_unread = make_action_item('mark_unread')
MarkUnread = mark_unread
mark_read = mark_unread
MarkRead = mark_unread
mark_as_read = mark_unread
MarkAsRead = mark_unread

star = make_action_item('star')
Star = star

delete = make_action_item('delete')
Delete = delete

mark_important = make_action_item('mark_important')
MarkImportant = mark_important

function add_category(cat)
    local val = cat
    if type(cat) == 'table' and cat.value ~= nil then
        val = cat.value
    end
    return { type = 'action_item', action = 'add_category', value = tostring(val) }
end
AddCategory = add_category
categorize = add_category
Categorize = add_category

function add_label(lbl) return { type = 'action_item', action = 'add_label', value = lbl } end
AddLabel = add_label
apply_label = add_label
ApplyLabel = add_label

function add_labels(...)
    local args = { ... }
    if #args == 1 and type(args[1]) == 'table' then
        return { type = 'action_item', action = 'add_labels', value = args[1] }
    else
        return { type = 'action_item', action = 'add_labels', value = args }
    end
end
AddLabels = add_labels
apply_labels = add_labels
ApplyLabels = add_labels

function forward_message(email) return { type = 'action_item', action = 'forward', value = tostring(email) } end
ForwardMessage = forward_message
forward = forward_message
Forward = forward_message

local ActionBuilder = {}
ActionBuilder.__index = ActionBuilder

function ActionBuilder:archive() table.insert(self.items, archive()); return self end
ActionBuilder.Archive = ActionBuilder.archive
function ActionBuilder:mark_unread() table.insert(self.items, mark_unread()); return self end
ActionBuilder.MarkUnread = ActionBuilder.mark_unread
ActionBuilder.mark_read = ActionBuilder.mark_unread
ActionBuilder.MarkRead = ActionBuilder.mark_unread
ActionBuilder.mark_as_read = ActionBuilder.mark_unread
ActionBuilder.MarkAsRead = ActionBuilder.mark_unread
ActionBuilder.read = ActionBuilder.mark_unread
ActionBuilder.Read = ActionBuilder.mark_unread
function ActionBuilder:star() table.insert(self.items, star()); return self end
ActionBuilder.Star = ActionBuilder.star
function ActionBuilder:delete() table.insert(self.items, delete()); return self end
ActionBuilder.Delete = ActionBuilder.delete
ActionBuilder.trash = ActionBuilder.delete
ActionBuilder.Trash = ActionBuilder.delete
function ActionBuilder:mark_important() table.insert(self.items, mark_important()); return self end
ActionBuilder.MarkImportant = ActionBuilder.mark_important
ActionBuilder.important = ActionBuilder.mark_important
ActionBuilder.Important = ActionBuilder.mark_important
function ActionBuilder:add_category(cat) table.insert(self.items, add_category(cat)); return self end
ActionBuilder.AddCategory = ActionBuilder.add_category
ActionBuilder.categorize = ActionBuilder.add_category
ActionBuilder.Categorize = ActionBuilder.add_category
function ActionBuilder:add_label(lbl) table.insert(self.items, add_label(lbl)); return self end
ActionBuilder.AddLabel = ActionBuilder.add_label
ActionBuilder.apply_label = ActionBuilder.add_label
ActionBuilder.ApplyLabel = ActionBuilder.add_label
function ActionBuilder:add_labels(...) table.insert(self.items, add_labels(...)); return self end
ActionBuilder.AddLabels = ActionBuilder.add_labels
ActionBuilder.apply_labels = ActionBuilder.add_labels
ActionBuilder.ApplyLabels = ActionBuilder.add_labels
function ActionBuilder:forward_message(email) table.insert(self.items, forward_message(email)); return self end
ActionBuilder.ForwardMessage = ActionBuilder.forward_message
ActionBuilder.forward = ActionBuilder.forward_message
ActionBuilder.Forward = ActionBuilder.forward_message

function ActionBuilder:build()
    return { type = 'action', items = self.items }
end

function action(arg)
    if arg == nil then
        return setmetatable({ type = 'action_builder', items = {} }, ActionBuilder)
    elseif type(arg) == 'table' then
        return setmetatable({ type = 'action', definition = arg }, {
            __index = arg
        })
    else
        return { type = 'action', definition = { arg } }
    end
end
Action = action

function actions(...)
    local args = { ... }
    if #args == 1 and type(args[1]) == 'table' and args[1].type ~= 'action_item' and args[1].action == nil then
        if args[1][1] ~= nil then
            return { type = 'action', items = args[1] }
        else
            return setmetatable({ type = 'action', definition = args[1] }, {
                __index = args[1]
            })
        end
    else
        return { type = 'action', items = args }
    end
end
Actions = actions

-- =======================================================================
-- Filter Builder & filter() DSL (Filter = Id + Query + Action)
-- =======================================================================
local FilterBuilder = {}
FilterBuilder.__index = function(tbl, key)
    -- Check FilterBuilder methods first
    local fbVal = rawget(FilterBuilder, key)
    if fbVal ~= nil then return fbVal end

    -- Forward query-related methods to QueryBuilder for fluent chaining
    local qbVal = rawget(QueryBuilder, key)
    if type(qbVal) == 'function' then
        return function(self, ...)
            qbVal(self, ...)
            return self
        end
    end

    return nil
end

function FilterBuilder:id(val) self.filter_id = tostring(val); return self end
FilterBuilder.Id = FilterBuilder.id

function FilterBuilder:name(val) self.filter_name = tostring(val); return self end
FilterBuilder.Name = FilterBuilder.name

function FilterBuilder:query(q) self.query_def = q; return self end
FilterBuilder.Query = FilterBuilder.query
FilterBuilder.criteria = FilterBuilder.query
FilterBuilder.Criteria = FilterBuilder.query
FilterBuilder.match = FilterBuilder.query
FilterBuilder.Match = FilterBuilder.query

function FilterBuilder:action(act) self.action_def = act; return self end
FilterBuilder.Action = FilterBuilder.action

function FilterBuilder:actions(...) self.action_def = actions(...); return self end
FilterBuilder.Actions = FilterBuilder.actions

function FilterBuilder:build()
    local q = self.query_def
    if q == nil and self.conditions ~= nil and #self.conditions > 0 then
        q = { type = 'query', conditions = self.conditions }
    end

    -- Return full filter structure
    return {
        type = 'filter',
        id = self.filter_id,
        name = self.filter_name,
        query = q,
        action = self.action_def
    }
end

function filter(arg)
    if arg == nil then
        return setmetatable({ type = 'filter_builder', conditions = {} }, FilterBuilder)
    elseif type(arg) == 'table' then
        return setmetatable({ type = 'filter', definition = arg }, {
            __index = arg
        })
    else
        return { type = 'filter', query = arg }
    end
end
Filter = filter

function rule(tbl)
    if type(tbl) == 'table' then
        return setmetatable({ type = 'rule', rule = tbl }, {
            __index = tbl
        })
    end
    return tbl
end
Rule = rule

-- =======================================================================
-- Label Builder & label() DSL
-- =======================================================================
local LabelBuilder = {}
LabelBuilder.__index = LabelBuilder

function LabelBuilder.new()
    local self = setmetatable({}, LabelBuilder)
    self._data = { type = 'gmail_label' }
    return self
end

function LabelBuilder:id(val)
    self._data.id = tostring(val)
    return self
end
LabelBuilder.Id = LabelBuilder.id

function LabelBuilder:name(val)
    self._data.name = tostring(val)
    return self
end
LabelBuilder.Name = LabelBuilder.name

function LabelBuilder:message_list_visibility(val)
    self._data.message_list_visibility = tostring(val)
    return self
end
LabelBuilder.MessageListVisibility = LabelBuilder.message_list_visibility
LabelBuilder.messageListVisibility = LabelBuilder.message_list_visibility

function LabelBuilder:show_in_message_list(val)
    if val == nil or val == true then
        self._data.message_list_visibility = 'show'
    else
        self._data.message_list_visibility = 'hide'
    end
    return self
end

function LabelBuilder:hide_in_message_list()
    self._data.message_list_visibility = 'hide'
    return self
end

function LabelBuilder:label_list_visibility(val)
    self._data.label_list_visibility = tostring(val)
    return self
end
LabelBuilder.LabelListVisibility = LabelBuilder.label_list_visibility
LabelBuilder.labelListVisibility = LabelBuilder.label_list_visibility

function LabelBuilder:show_in_label_list()
    self._data.label_list_visibility = 'labelShow'
    return self
end

function LabelBuilder:show_if_unread()
    self._data.label_list_visibility = 'labelShowIfUnread'
    return self
end

function LabelBuilder:hide_in_label_list()
    self._data.label_list_visibility = 'labelHide'
    return self
end

function LabelBuilder:color(arg1, arg2)
    if type(arg1) == 'table' then
        self._data.color = arg1
    elseif arg1 ~= nil and arg2 ~= nil then
        self._data.color = { textColor = tostring(arg1), backgroundColor = tostring(arg2) }
    end
    return self
end
LabelBuilder.Color = LabelBuilder.color

function LabelBuilder:text_color(val)
    self._data.color = self._data.color or {}
    self._data.color.textColor = tostring(val)
    return self
end
LabelBuilder.TextColor = LabelBuilder.text_color

function LabelBuilder:background_color(val)
    self._data.color = self._data.color or {}
    self._data.color.backgroundColor = tostring(val)
    return self
end
LabelBuilder.BackgroundColor = LabelBuilder.background_color

function LabelBuilder:build()
    return self._data
end

function color(arg1, arg2)
    if type(arg1) == 'table' then
        return {
            type = 'label_color',
            textColor = arg1.text or arg1.text_color or arg1.textColor,
            backgroundColor = arg1.background or arg1.background_color or arg1.backgroundColor or arg1.bg
        }
    elseif arg1 ~= nil and arg2 ~= nil then
        return { type = 'label_color', textColor = tostring(arg1), backgroundColor = tostring(arg2) }
    end
    return { type = 'label_color' }
end
Color = color

function label(arg)
    if arg == nil then
        return LabelBuilder.new()
    elseif type(arg) == 'string' then
        return { type = 'field', field = 'label', value = tostring(arg) }
    elseif type(arg) == 'table' then
        arg.type = 'gmail_label'
        return setmetatable(arg, { __index = arg })
    end
    return { type = 'field', field = 'label', value = tostring(arg) }
end
Label = label
gmail_label = label
user_label = label

function labels(...)
    local args = { ... }
    if #args == 1 and type(args[1]) == 'table' and args[1].type ~= 'gmail_label' and args[1].name == nil then
        return args[1]
    else
        return args
    end
end
Labels = labels
";

    private static readonly Regex KeywordRewriteRegex = new(
        @"(""(\\.|[^""\\])*""|'(\\.|[^'\\])*'|\[\[.*?\]\]|--\[\[.*?\]\]|--[^\r\n]*)|\b(?<kw>not|in)\s*\(",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public static string PreprocessScript(string script)
    {
        if (string.IsNullOrEmpty(script)) return script;
        return KeywordRewriteRegex.Replace(script, m =>
        {
            if (m.Groups[1].Success) return m.Groups[1].Value;
            string kw = m.Groups["kw"].Value.ToLowerInvariant();
            return kw == "in" ? "In(" : "Not(";
        });
    }

    private static void RegisterHelpers(LuaState state)
    {
        state.Environment["env"] = new LuaFunction((context, ct) =>
        {
            var key = context.GetArgument<string>(0);
            var defaultVal = context.HasArgument(1) ? context.GetArgument<string>(1) : string.Empty;
            var val = Environment.GetEnvironmentVariable(key) ?? defaultVal;
            return ValueTask.FromResult(context.Return(val));
        });

        state.Environment["platform"] = new LuaFunction((context, ct) =>
        {
            string os = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                      : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                      : "Unknown";
            return ValueTask.FromResult(context.Return(os));
        });
    }

    private static string? ExtractDateFormat(LuaTable root, LuaState state)
    {
        if (root.TryGetValue("date_format", out var dfVal) && dfVal.Type == LuaValueType.String)
        {
            return dfVal.Read<string>();
        }
        if (root.TryGetValue("DateFormat", out var dfValUpper) && dfValUpper.Type == LuaValueType.String)
        {
            return dfValUpper.Read<string>();
        }
        if (root.TryGetValue("settings", out var sVal) && sVal.TryRead<LuaTable>(out var sTable) &&
            sTable.TryGetValue("date_format", out var sdfVal) && sdfVal.Type == LuaValueType.String)
        {
            return sdfVal.Read<string>();
        }
        if (state.Environment.TryGetValue("date_format", out var envDf) && envDf.Type == LuaValueType.String)
        {
            return envDf.Read<string>();
        }
        return null;
    }

    private async Task<(LuaTable rootTable, string? customDateFormat)> ExecuteScriptAsync(string script, LuaState? externalState)
    {
        var state = externalState ?? LuaState.Create();
        state.OpenStandardLibraries();
        RegisterHelpers(state);

        await state.DoStringAsync(DslPrelude);

        string preprocessed = PreprocessScript(script);

        LuaValue[] results;
        try
        {
            results = await state.DoStringAsync(preprocessed);
        }
        catch (Exception ex)
        {
            throw new LuaConfigException($"Failed to evaluate Lua filter script: {ex.Message}", ex);
        }

        LuaTable rootTable;
        if (results.Length > 0 && results[0].TryRead<LuaTable>(out var retTable))
        {
            rootTable = retTable;
        }
        else if (state.Environment.TryGetValue("config", out var gCfg) && gCfg.TryRead<LuaTable>(out var gTable))
        {
            rootTable = gTable;
        }
        else if (state.Environment.TryGetValue("rule", out var gRule) && gRule.TryRead<LuaTable>(out var rTable))
        {
            rootTable = rTable;
        }
        else if (state.Environment.TryGetValue("filter", out var gFilter) && gFilter.TryRead<LuaTable>(out var fTable))
        {
            rootTable = fTable;
        }
        else if (state.Environment.TryGetValue("query", out var gQuery) && gQuery.TryRead<LuaTable>(out var qTable))
        {
            rootTable = qTable;
        }
        else if (state.Environment.TryGetValue("label", out var gLabel) && gLabel.TryRead<LuaTable>(out var lTable))
        {
            rootTable = lTable;
        }
        else if (state.Environment.TryGetValue("labels", out var gLabels) && gLabels.TryRead<LuaTable>(out var lsTable))
        {
            rootTable = lsTable;
        }
        else
        {
            throw new LuaConfigException("Lua script must return a table or define a global 'filter', 'rule', 'query', 'label', or 'config' table.");
        }

        string? customDateFormat = ExtractDateFormat(rootTable, state);
        return (rootTable, customDateFormat);
    }

    // =========================================================================
    // Filter Loading (Filter = Id + Query + Action)
    // =========================================================================

    public async Task<GmailFilter> LoadFilterFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Filter configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadFilterFromScriptAsync(script, externalState);
    }

    public async Task<GmailFilter> LoadFilterFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        return GmailFilterParser.ParseFilter(rootTable, customDateFormat);
    }

    public async Task<List<GmailFilter>> LoadFiltersFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Filter configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadFiltersFromScriptAsync(script, externalState);
    }

    public async Task<List<GmailFilter>> LoadFiltersFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        return ParseFiltersFromRoot(rootTable, customDateFormat);
    }

    private static List<GmailFilter> ParseFiltersFromRoot(LuaTable root, string? customDateFormat)
    {
        var filters = new List<GmailFilter>();

        // Check if root has "filters" or "rules" table: { filters = { ... } } or { rules = { ... } }
        foreach (var key in new[] { "filters", "rules" })
        {
            if (root.TryGetValue(key, out var listVal) && listVal.TryRead<LuaTable>(out var listTable))
            {
                for (int i = 1; i <= listTable.ArrayLength; i++)
                {
                    if (listTable[i].TryRead<LuaTable>(out var itemTable))
                    {
                        filters.Add(GmailFilterParser.ParseFilter(itemTable, customDateFormat));
                    }
                }
                foreach (var pair in listTable)
                {
                    if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var itemTable))
                    {
                        filters.Add(GmailFilterParser.ParseFilter(itemTable, customDateFormat));
                    }
                }
                if (filters.Count > 0) return filters;
            }
        }

        // Check if root is an array of filters/rules: { filter1, filter2 }
        if (root.ArrayLength > 0 && root[1].Type == LuaValueType.Table)
        {
            for (int i = 1; i <= root.ArrayLength; i++)
            {
                if (root[i].TryRead<LuaTable>(out var itemTable))
                {
                    filters.Add(GmailFilterParser.ParseFilter(itemTable, customDateFormat));
                }
            }
            if (filters.Count > 0) return filters;
        }

        // Single filter
        filters.Add(GmailFilterParser.ParseFilter(root, customDateFormat));
        return filters;
    }

    // =========================================================================
    // Rule Loading (Backward Compatibility: Rule = Filter with metadata)
    // =========================================================================

    public async Task<List<GmailRule>> LoadRulesFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Filter configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadRulesFromScriptAsync(script, externalState);
    }

    public async Task<GmailRule> LoadRuleFromScriptAsync(string script, LuaState? externalState = null)
    {
        var rules = await LoadRulesFromScriptAsync(script, externalState);
        if (rules.Count == 0)
        {
            throw new LuaConfigException("No Gmail rules found in configuration script.");
        }
        return rules[0];
    }

    public async Task<List<GmailRule>> LoadRulesFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        return ParseRulesFromRoot(rootTable, customDateFormat);
    }

    private static List<GmailRule> ParseRulesFromRoot(LuaTable root, string? customDateFormat)
    {
        var rules = new List<GmailRule>();

        foreach (var key in new[] { "rules", "filters" })
        {
            if (root.TryGetValue(key, out var listVal) && listVal.TryRead<LuaTable>(out var listTable))
            {
                for (int i = 1; i <= listTable.ArrayLength; i++)
                {
                    if (listTable[i].TryRead<LuaTable>(out var itemTable))
                    {
                        rules.Add(GmailFilterParser.ParseRule(itemTable, customDateFormat));
                    }
                }
                foreach (var pair in listTable)
                {
                    if (pair.Key.Type != LuaValueType.Number && pair.Value.TryRead<LuaTable>(out var itemTable))
                    {
                        rules.Add(GmailFilterParser.ParseRule(itemTable, customDateFormat));
                    }
                }
                if (rules.Count > 0) return rules;
            }
        }

        if (root.ArrayLength > 0 && root[1].Type == LuaValueType.Table)
        {
            for (int i = 1; i <= root.ArrayLength; i++)
            {
                if (root[i].TryRead<LuaTable>(out var itemTable))
                {
                    rules.Add(GmailFilterParser.ParseRule(itemTable, customDateFormat));
                }
            }
            if (rules.Count > 0) return rules;
        }

        rules.Add(GmailFilterParser.ParseRule(root, customDateFormat));
        return rules;
    }

    // =========================================================================
    // Query Loading (Query = Search criteria / condition)
    // =========================================================================

    public async Task<IQueryCondition> LoadQueryFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Query configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadQueryFromScriptAsync(script, externalState);
    }

    public async Task<IQueryCondition> LoadQueryFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        return GmailQueryParser.ParseQuery(rootTable, customDateFormat);
    }

    public async Task<List<IQueryCondition>> LoadQueriesFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Query configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadQueriesFromScriptAsync(script, externalState);
    }

    public async Task<List<IQueryCondition>> LoadQueriesFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        var queries = new List<IQueryCondition>();

        if (rootTable.TryGetValue("queries", out var qVal) && qVal.TryRead<LuaTable>(out var qTable))
        {
            for (int i = 1; i <= qTable.ArrayLength; i++)
            {
                if (qTable[i].TryRead<LuaTable>(out var itemTable))
                {
                    queries.Add(GmailQueryParser.ParseQuery(itemTable, customDateFormat));
                }
            }
            if (queries.Count > 0) return queries;
        }

        if (rootTable.ArrayLength > 0 && rootTable[1].Type == LuaValueType.Table)
        {
            for (int i = 1; i <= rootTable.ArrayLength; i++)
            {
                if (rootTable[i].TryRead<LuaTable>(out var itemTable))
                {
                    queries.Add(GmailQueryParser.ParseQuery(itemTable, customDateFormat));
                }
            }
            if (queries.Count > 0) return queries;
        }

        queries.Add(GmailQueryParser.ParseQuery(rootTable, customDateFormat));
        return queries;
    }

    // =========================================================================
    // Action Loading (Action = Actions performed on messages)
    // =========================================================================

    public async Task<GmailAction> LoadActionFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Action configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadActionFromScriptAsync(script, externalState);
    }

    public async Task<GmailAction> LoadActionFromScriptAsync(string script, LuaState? externalState = null)
    {
        var state = externalState ?? LuaState.Create();
        state.OpenStandardLibraries();
        RegisterHelpers(state);

        await state.DoStringAsync(DslPrelude);

        string preprocessed = PreprocessScript(script);
        var results = await state.DoStringAsync(preprocessed);

        if (results.Length > 0 && results[0].Type == LuaValueType.String)
        {
            return GmailActionParser.ParseActionString(results[0].Read<string>());
        }

        LuaTable? table = null;
        if (results.Length > 0 && results[0].TryRead<LuaTable>(out var resTable))
        {
            table = resTable;
        }
        else if (state.Environment.TryGetValue("action", out var gAct) && gAct.TryRead<LuaTable>(out var aTable))
        {
            table = aTable;
        }
        else if (state.Environment.TryGetValue("actions", out var gActs) && gActs.TryRead<LuaTable>(out var asTable))
        {
            table = asTable;
        }

        if (table == null)
        {
            throw new LuaConfigException("Lua action script must return an action expression or define an 'action' table.");
        }

        if (table.TryGetValue("action", out var innerAct) && innerAct.TryRead<LuaTable>(out var innerActTable))
        {
            return GmailActionParser.ParseAction(innerActTable);
        }
        if (table.TryGetValue("actions", out var innerActs) && innerActs.TryRead<LuaTable>(out var innerActsTable))
        {
            return GmailActionParser.ParseAction(innerActsTable);
        }

        return GmailActionParser.ParseAction(table);
    }

    // =========================================================================
    // Label Loading
    // =========================================================================

    public async Task<GmailLabel> LoadLabelFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Label configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadLabelFromScriptAsync(script, externalState);
    }

    public async Task<GmailLabel> LoadLabelFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, _) = await ExecuteScriptAsync(script, externalState);
        return GmailLabelParser.ParseLabel(rootTable);
    }

    public async Task<List<GmailLabel>> LoadLabelsFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Label configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadLabelsFromScriptAsync(script, externalState);
    }

    public async Task<List<GmailLabel>> LoadLabelsFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, _) = await ExecuteScriptAsync(script, externalState);
        return ParseLabelsFromRoot(rootTable);
    }

    public static List<GmailLabel> ParseLabelsFromRoot(LuaTable root)
    {
        var labels = new List<GmailLabel>();

        // Check if root has "labels" table: { labels = { ... } }
        if (root.TryGetValue("labels", out var listVal) && listVal.TryRead<LuaTable>(out var listTable))
        {
            for (int i = 1; i <= listTable.ArrayLength; i++)
            {
                var elem = listTable[i];
                if (elem.TryRead<LuaTable>(out var itemTable))
                {
                    labels.Add(GmailLabelParser.ParseLabel(itemTable));
                }
            }
            return labels;
        }

        // Check if root is an array of labels: { label { ... }, label { ... } }
        if (root.ArrayLength > 0)
        {
            for (int i = 1; i <= root.ArrayLength; i++)
            {
                var elem = root[i];
                if (elem.TryRead<LuaTable>(out var itemTable))
                {
                    labels.Add(GmailLabelParser.ParseLabel(itemTable));
                }
            }
            return labels;
        }

        // If root is a single label
        if (GmailLabelParser.IsLabelTable(root))
        {
            labels.Add(GmailLabelParser.ParseLabel(root));
            return labels;
        }

        return labels;
    }

    // =========================================================================
    // Full Configuration Loading (Filters + Labels)
    // =========================================================================

    public async Task<GmailConfiguration> LoadConfigurationFromFileAsync(string filePath, LuaState? externalState = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        string script = await File.ReadAllTextAsync(filePath);
        return await LoadConfigurationFromScriptAsync(script, externalState);
    }

    public async Task<GmailConfiguration> LoadConfigurationFromScriptAsync(string script, LuaState? externalState = null)
    {
        var (rootTable, customDateFormat) = await ExecuteScriptAsync(script, externalState);
        var filters = ParseFiltersFromRoot(rootTable, customDateFormat);
        var labels = ParseLabelsFromRoot(rootTable);

        return new GmailConfiguration
        {
            Filters = filters,
            Labels = labels,
            CustomDateFormat = customDateFormat
        };
    }
}

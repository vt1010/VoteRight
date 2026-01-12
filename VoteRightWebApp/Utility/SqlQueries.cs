namespace VoteRightWebApp.Utility;

public static class SqlQueries
{
    public static class Metadata
    {
        public const string DistinctDistricts = @"SELECT DISTINCT district
                                                  FROM public.metadata
                                                  WHERE district IS NOT NULL AND district <> ''
                                                  ORDER BY district";
    }

    public static class Users
    {
        public const string FindByPhone = @"SELECT id, name, phone_number, district
                                           FROM public.users WHERE phone_number = @phone LIMIT 1";

        public const string Insert = @"INSERT INTO public.users (name, phone_number, whatsapp_number, district, political_party_organization, organizational_position, registered_at)
                                       VALUES (@name, @phone, @wa, @district, @org, @pos, @reg)
                                       RETURNING id";

        public const string SelectByDistrict = @"SELECT DISTINCT id, name, phone_number, whatsapp_number, district, political_party_organization, organizational_position, registered_at
                                                FROM public.users WHERE district = @district";

        public const string SelectByDistrictAndAssemblyJoinDownloads = @"SELECT DISTINCT u.id, u.name, u.phone_number, u.whatsapp_number, u.district, u.political_party_organization, u.organizational_position, u.registeredAt
                                                                         FROM public.users u INNER JOIN public.filedownloads d ON u.id = d.userId
                                                                         WHERE u.district = @district AND d.assembly = @assembly";
    }

    public static class Downloads
    {
        public const string Insert = @"INSERT INTO public.filedownloads (user_id, assembly, booths, device_type, downloaded_at)
                                      VALUES (@userId, @assembly, @booths, @deviceType, @downloadedAt)
                                      RETURNING id";
    }

    public static class Assemblies
    {                
        public const string DistinctByDistrictWithBoothCount = @"SELECT
                                                                                                        m.constituency_details->>'assembly_constituency_name'   AS name,
                                                                                                        m.constituency_details->>'assembly_constituency_number' AS number,
                                                                                                        COUNT(DISTINCT m.constituency_details->>'part_number')  AS booth_count
                                                                                                     FROM public.metadata m
                                                                                                     WHERE NULLIF(m.constituency_details->>'assembly_constituency_name', '') IS NOT NULL
                                                                                                         AND NULLIF(m.constituency_details->>'assembly_constituency_number', '') IS NOT NULL
                                                                                                         AND NULLIF(m.district, '') IS NOT NULL
                                                                                                         AND m.district = @district
                                                                                                     GROUP BY
                                                                                                             m.constituency_details->>'assembly_constituency_name',
                                                                                                             m.constituency_details->>'assembly_constituency_number'
                                                                                                     ORDER BY CAST(m.constituency_details->>'assembly_constituency_number' AS INTEGER)";
    }

    public static class Voters
    {
        public const string SelectByAssemblyBase = @"SELECT document_id, serial_no, epic_no, name, relation_type, father_name, mother_name,
                                                            husband_name, other_name, house_no, age, gender, street_names_and_numbers,
                                                            part_no, assembly, epic_valid, deleted
                                                     FROM public.voters
                                                     WHERE assembly LIKE @assembly";

        public const string RangeClause = " AND CAST(part_no AS INTEGER) BETWEEN @start AND @end";
        public const string OrderByPartNoSerialNo = " ORDER BY part_no, serial_no";
    }
}
